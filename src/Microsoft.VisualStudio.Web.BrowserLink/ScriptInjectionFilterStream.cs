// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    /// <summary>
    /// This stream implementation is a passthrough filter. It's job is to add
    /// links to the Browser Link connection scripts at the end of HTML content.
    /// 
    /// It does this using a connection to the host, where the actual filtering
    /// work is done. If anything goes wrong with the host connection, or
    /// if the content being written is not actually HTML, then the filter goes
    /// into passthrough mode and returns all content to the output stream unchanged.
    /// </summary>
    internal class ScriptInjectionFilterStream : Stream
    {
        private enum FilterState
        {
            NothingSentToFilter,
            ContentSentToFilter,
            Passthrough,
        }

        private IHttpSocketAdapter _injectScriptSocket;
        private IScriptInjectionFilterContext _context;
        private Stream _outputStream;

        private FilterState _filterState = FilterState.NothingSentToFilter;

        internal ScriptInjectionFilterStream(IHttpSocketAdapter injectScriptSocket, IScriptInjectionFilterContext context)
        {
            _context = context;
            _outputStream = _context.ResponseBody;
            _injectScriptSocket = injectScriptSocket;

            _injectScriptSocket.SetResponseHandler(CreateResponseHandler(_outputStream));
        }

        /// <summary>
        /// Returns true if the filter is in Passthrough mode. All writes will
        /// be passed to the output stream unmodified.
        /// </summary>
        public bool IsPassthrough
        {
            get { return _filterState == FilterState.Passthrough; }
        }

        /// <summary>
        /// Returns true if any data has been sent to the host. At this point,
        /// the filter cannot go into passthrough mode to handle a failure, because
        /// the data that was sent to the host is lost.
        /// </summary>
        public bool SentContentToFilter
        {
            get { return _filterState == FilterState.ContentSentToFilter; }
        }

        /// <summary>
        /// Returns true if the initial request to the host timed out without
        /// returning a response code. 
        /// </summary>
        public bool ScriptInjectionTimedOut
        {
            get;
            private set;
        }

        /// <summary>
        /// Call this method to signal the host that all the content for filtering
        /// has been sent, and then wait for the host to return the filtered content.
        /// </summary>
        /// <returns>A task that completes when the host has returned the filtered contet.</returns>
        public async Task WaitForFilterComplete()
        {
            if (SentContentToFilter)
            {
                await _injectScriptSocket.CompleteRequest();

                await _injectScriptSocket.WaitForResponseComplete();
            }

            CloseInjectScriptSocketAndBecomePassthrough();
        }

        protected override void Dispose(bool disposing)
        {
            CloseInjectScriptSocketAndBecomePassthrough();
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override void Flush()
        {
            Task.WaitAll(FlushAsync());
        }

        /// <remarks>
        /// In order to flush the content to the output stream, we need to wait
        /// for the content that was sent to the host to return. Once that
        /// is done, the connection to the host is closed, and the stream
        /// goes into passthrough mode. So effectively, any writes that come
        /// after a call to flush will not be filtered.
        /// </remarks>
        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await WaitForFilterComplete();

            await _outputStream.FlushAsync();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Task.WaitAll(WriteAsync(buffer, offset, count));
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            bool firstWrite = (_filterState == FilterState.NothingSentToFilter);

            if (firstWrite)
            {
                DetermineIfFilterShouldBePassthrough(buffer, offset, count);

                AddResponseHeadersToFilterRequest();
            }

            if (IsPassthrough)
            {
                return WriteToOutputStreamAsync(buffer, offset, count, ref cancellationToken);
            }
            else
            {
                _filterState = FilterState.ContentSentToFilter;

                return WriteToInjectScriptSocketAsync(firstWrite, buffer, offset, count, cancellationToken);
            }
        }

        private Task WriteToOutputStreamAsync(byte[] buffer, int offset, int count, ref CancellationToken cancellationToken)
        {
            return _outputStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        private Task WriteToInjectScriptSocketAsync(bool firstWrite, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (firstWrite)
            {
                return FirstWriteToInjectScriptSocketAsync(buffer, offset, count, cancellationToken);
            }
            else
            {
                return _injectScriptSocket.WriteToRequestAsync(buffer, offset, count);
            }
        }

        private async Task FirstWriteToInjectScriptSocketAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                await _injectScriptSocket.WriteToRequestAsync(buffer, offset, count);

                Task<int> statusCodeTask = _injectScriptSocket.GetResponseStatusCode();

                int statusCode = await TaskHelpers.WaitWithTimeout(
                    statusCodeTask,
                    timeout: TimeSpan.FromMilliseconds(1000),
                    resultIfTimedOut: 504 /*Gateway Timeout*/);

                if (statusCode == 200)
                {
                    return;
                }

                if (statusCode == 504)
                {
                    ScriptInjectionTimedOut = true;
                }
            }
            catch
            {
                // Fall through to error case
            }

            // If the initial send fails, we can switch to passthrough mode
            // and retry this write. Browser Link won't work, but at least
            // the page will be returned to the browser.
            CloseInjectScriptSocketAndBecomePassthrough();

            await WriteAsync(buffer, offset, count, cancellationToken);
        }

        private static ResponseHandler CreateResponseHandler(Stream outputStream)
        {
            // The host will send a well-known preamble before sending response data.
            // The purpose is just to get the headers back before processing starts.
            // The preamble should be skipped.

            bool skippedFilterPreamble = false;

            return async delegate (byte[] buffer, int offset, int count)
            {
                if (!skippedFilterPreamble)
                {
                    offset += BrowserLinkConstants.FilterPreamble.Length;
                    count -= BrowserLinkConstants.FilterPreamble.Length;

                    skippedFilterPreamble = true;
                }

                if (count > 0)
                {
                    await outputStream.WriteAsync(buffer, offset, count);
                }
            };
        }

        private void AddResponseHeadersToFilterRequest()
        {
            if (!IsPassthrough)
            {
                _injectScriptSocket.AddRequestHeader("Content-Type", _context.ResponseContentType);
            }
        }

        private void DetermineIfFilterShouldBePassthrough(byte[] buffer, int offset, int count)
        {
            string contentTypeFromHeader = _context.ResponseContentType;
            string path = _context.RequestPath;

            if (!ContentTypeUtil.IsXhtml(contentTypeFromHeader) && !ContentTypeUtil.IsHtml(contentTypeFromHeader))
            {
                CloseInjectScriptSocketAndBecomePassthrough();
            }

            else if (!ContentTypeUtil.IsHtml(path, buffer, offset, count))
            {
                CloseInjectScriptSocketAndBecomePassthrough();
            }
        }

        private void CloseInjectScriptSocketAndBecomePassthrough()
        {
            if (_injectScriptSocket != null)
            {
                _injectScriptSocket.Dispose();
                _injectScriptSocket = null;
            }

            _filterState = FilterState.Passthrough;
        }
    }
}