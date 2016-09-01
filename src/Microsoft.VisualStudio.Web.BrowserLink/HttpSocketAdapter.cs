// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    internal delegate Task ResponseHandler(byte[] buffer, int offset, int count);

    /// <summary>
    /// HttpSocketAdapter wraps an ISocketAdapter, and provides functionality
    /// for making an HTTP request and handling an HTTP reponse on the socket.
    /// </summary>
    internal interface IHttpSocketAdapter : IDisposable
    {
        /// <summary>
        /// Adds a header to the HTTP request. If this method is called after
        /// the first call to WriteToRequestAsync, it is ignored.
        /// </summary>
        /// <param name="name">The name of the header</param>
        /// <param name="value">The value of the header</param>
        void AddRequestHeader(string name, string value);

        /// <summary>
        /// Sends some request data. Data is sent using chunked transfer-encoding,
        /// so there can be multiple calls to this method.
        /// </summary>
        /// <param name="buffer">Buffer containing the data.</param>
        /// <param name="offset">The position in the buffer where the data starts.</param>
        /// <param name="count">The number of bytes of data in the buffer.</param>
        /// <returns>A task that completes when the data has been sent.</returns>
        Task WriteToRequestAsync(byte[] buffer, int offset, int count);

        /// <summary>
        /// Sends data signifying the end of the request. This should only be
        /// called once. Any subsequent calls to WriteToRequestAsync will probably
        /// be ignored by the server, and may result in exceptions if the socket
        /// is closed.
        /// </summary>
        /// <returns>A task that completes when the data has been sent (not when the response is complete).</returns>
        Task CompleteRequest();

        /// <summary>
        /// Gets the HTTP status code returned by the server.
        /// </summary>
        /// <returns>A task that completes when the status code has been returned by the server. The task returns the status code.</returns>
        Task<int> GetResponseStatusCode();

        /// <summary>
        /// Gets the value of a header in the HTTP response from the server.
        /// </summary>
        /// <param name="headerName">The case-insensitive name of the header.</param>
        /// <returns>A task that completes when all headers have been returned by the server. The task returns the value of the requested header.</returns>
        Task<string> GetResponseHeader(string headerName);

        /// <summary>
        /// Sets a callback method that will be called with data contained in the
        /// response body returned by the server.
        /// </summary>
        void SetResponseHandler(ResponseHandler handler);

        /// <summary>
        /// Wait until the response body has been completely returned by the server.
        /// </summary>
        /// <returns>A task that completes when the response body has been completely returned by the server.</returns>
        Task WaitForResponseComplete();
    }

    internal class HttpSocketAdapter : IHttpSocketAdapter
    {
        private static readonly ArraySegment<byte> CrlfBuffer = CreateBufferContaining("\r\n", Encoding.ASCII);

        private ISocketAdapter _socket;
        private ResponseReader _responseReader;

        private StringBuilder _headerSection = new StringBuilder();

        public HttpSocketAdapter(string httpMethod, Uri url, ISocketAdapter socket)
        {
            _socket = socket;

            string requestUri = url.GetComponents(UriComponents.PathAndQuery | UriComponents.Fragment, UriFormat.UriEscaped);

            _headerSection.AppendFormat("{0} {1} HTTP/1.1\r\n", httpMethod, requestUri);

            AddRequestHeader("Host", url.GetComponents(UriComponents.HostAndPort, UriFormat.UriEscaped));
            AddRequestHeader("Transfer-Encoding", "chunked");
            AddRequestHeader("Connection", "keep-alive");
        }

        public void Dispose()
        {
            if (_responseReader != null)
            {
                _responseReader.Stop();
                _responseReader.Dispose();
                _responseReader = null;
            }

            if (_socket != null)
            {
                _socket.Dispose();
                _socket = null;
            }
        }

        public static async Task<IHttpSocketAdapter> OpenHttpSocketAsync(string httpMethod, Uri url)
        {
            ISocketAdapter socket = await SocketAdapter.OpenSocketAsync(url);

            if (socket != null)
            {
                return new HttpSocketAdapter(httpMethod, url, socket);
            }

            return null;
        }

        public void AddRequestHeader(string name, string value)
        {
            if (_headerSection != null)
            {
                _headerSection.AppendFormat("{0}: {1}\r\n", name, value);
            }
        }

        public Task<int> GetResponseStatusCode()
        {
            return GetResponseReader().GetResponseStatusCode();
        }

        public async Task<string> GetResponseHeader(string headerName)
        {
            Dictionary<string, string> responseHeaders = await GetResponseReader().GetResponseHeaders();

            foreach (KeyValuePair<string, string> responseHeader in responseHeaders)
            {
                if (responseHeader.Key.Equals(headerName, StringComparison.OrdinalIgnoreCase))
                {
                    return responseHeader.Value;
                }
            }

            return null;
        }

        public void SetResponseHandler(ResponseHandler handler)
        {
            GetResponseReader().SetResponseHandler(handler);
        }

        public Task WaitForResponseComplete()
        {
            return GetResponseReader().WaitForResponseComplete();
        }

        private static ArraySegment<byte> CreateBufferContaining(string text, Encoding encoding)
        {
            return new ArraySegment<byte>(encoding.GetBytes(text));
        }

        public Task WriteToRequestAsync(byte[] buffer, int offset, int count)
        {
            List<ArraySegment<byte>> bufferList = new List<ArraySegment<byte>>();

            AddRequestBuffersIfNecessary(bufferList);

            if (count > 0)
            {
                bufferList.Add(CreateBufferContaining(count.ToString("X"), Encoding.ASCII));
                bufferList.Add(CrlfBuffer);
                bufferList.Add(new ArraySegment<byte>(buffer, offset, count));
                bufferList.Add(CrlfBuffer);
            }

            return _socket.SendAsync(bufferList);
        }

        public Task CompleteRequest()
        {
            List<ArraySegment<byte>> bufferList = new List<ArraySegment<byte>>();

            AddRequestBuffersIfNecessary(bufferList);

            bufferList.Add(CreateBufferContaining("0", Encoding.ASCII));
            bufferList.Add(CrlfBuffer);
            bufferList.Add(CrlfBuffer);

            return _socket.SendAsync(bufferList);
        }

        private void AddRequestBuffersIfNecessary(List<ArraySegment<byte>> bufferList)
        {
            if (_headerSection != null)
            {
                bufferList.Add(CreateBufferContaining(_headerSection.ToString(), Encoding.ASCII));
                bufferList.Add(CrlfBuffer);

                _headerSection = null;
            }
        }

        private ResponseReader GetResponseReader()
        {
            if (_responseReader == null)
            {
                _responseReader = new ResponseReader(_socket);
            }

            return _responseReader;
        }

        private class ResponseReader : IDisposable
        {
            private static readonly char[] StatusLineSeparators = new char[] { ' ' };
            private static readonly char[] HeaderLineSeparators = new char[] { ':' };

            private SocketReader _socketReader;

            private CancellationTokenSource _cancelSource = new CancellationTokenSource();

            private TaskCompletionSource<int> _statusCodeTask = new TaskCompletionSource<int>();
            private TaskCompletionSource<Dictionary<string, string>> _responseHeadersTask = new TaskCompletionSource<Dictionary<string, string>>();
            private TaskCompletionSource<ResponseHandler> _setResponseHandlerTask = new TaskCompletionSource<ResponseHandler>();

            private Task _responseCompleteTask;

            public ResponseReader(ISocketAdapter socket)
            {
                _socketReader = new SocketReader(socket);

                _responseCompleteTask = ReadResponse();
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_cancelSource",
                Justification = "The CancellationTokenSource can't be disposed until _responseCompleteTask is done with it. But it will be disposed.")]
            public void Dispose()
            {
                _responseCompleteTask.ContinueWith(completeTask =>
                {
                    _cancelSource.Dispose();
                });
            }

            internal void Stop()
            {
                _cancelSource.Cancel();
            }

            internal Task<int> GetResponseStatusCode()
            {
                return _statusCodeTask.Task;
            }

            internal Task<Dictionary<string, string>> GetResponseHeaders()
            {
                return _responseHeadersTask.Task;
            }

            internal void SetResponseHandler(ResponseHandler handler)
            {
                _setResponseHandlerTask.SetResult(handler);
            }

            internal Task WaitForResponseComplete()
            {
                return _responseCompleteTask;
            }

            private string GetResponseHeader(string headerName)
            {
                foreach (KeyValuePair<string, string> responseHeader in GetResponseHeaders().Result)
                {
                    if (responseHeader.Key.Equals(headerName, StringComparison.OrdinalIgnoreCase))
                    {
                        return responseHeader.Value;
                    }
                }

                return null;
            }

            private bool HasResponseHeader(string headerName, string headerValue)
            {
                string actualHeaderValue = GetResponseHeader(headerName);

                if (actualHeaderValue == null)
                {
                    return false;
                }

                return String.Equals(headerValue, actualHeaderValue, StringComparison.OrdinalIgnoreCase);
            }

            private async Task ReadResponse()
            {
                try
                {
                    _statusCodeTask.SetResult(await ReadStatusLine());

                    _responseHeadersTask.SetResult(await ReadHeaders());

                    if (HasResponseHeader("Transfer-Encoding", "chunked"))
                    {
                        await ReadChunkedContent();
                    }
                    else
                    {
                        await ReadContent();
                    }
                }
                catch (Exception ex)
                {
                    _statusCodeTask.TrySetException(ex);
                    _responseHeadersTask.TrySetException(ex);

                    throw;
                }
            }

            private async Task<int> ReadStatusLine()
            {
                string statusLine = await _socketReader.ReadLine(_cancelSource.Token);

                string[] parts = statusLine.Split(StatusLineSeparators, count: 3);

                if (parts.Length >= 2)
                {
                    int statusCode;

                    if (Int32.TryParse(parts[1], out statusCode))
                    {
                        return statusCode;
                    }
                }

                // Status line was in an unexpected format. Assume "500: Internal Server Error"
                return 500;
            }

            private async Task<Dictionary<string, string>> ReadHeaders()
            {
                Dictionary<string, string> responseHeaders = new Dictionary<string, string>();

                while (true)
                {
                    string headerLine = await _socketReader.ReadLine(_cancelSource.Token);

                    if (String.IsNullOrEmpty(headerLine))
                    {
                        // End of headers
                        return responseHeaders;
                    }

                    string[] parts = headerLine.Split(HeaderLineSeparators, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        responseHeaders[parts[0]] = parts[1].Trim();
                    }
                }
            }

            private async Task ReadChunkedContent()
            {
                while (true)
                {
                    string contentLengthLine = await _socketReader.ReadLine(_cancelSource.Token);
                    int contentLength = Int32.Parse(contentLengthLine, System.Globalization.NumberStyles.HexNumber);

                    if (contentLength == 0)
                    {
                        return;
                    }

                    await ReadBytesIntoResponse(contentLength);

                    await _socketReader.ReadLine(_cancelSource.Token);
                }
            }

            private Task ReadContent()
            {
                string contentLengthString = GetResponseHeader("Content-Length");

                if (String.IsNullOrEmpty(contentLengthString))
                {
                    return StaticTaskResult.Zero;
                }

                long contentLength;

                if (!Int64.TryParse(contentLengthString, out contentLength))
                {
                    return StaticTaskResult.Zero;
                }

                return ReadBytesIntoResponse(contentLength);
            }

            private async Task ReadBytesIntoResponse(long bytesToRead)
            {
                ResponseHandler handler = await _setResponseHandlerTask.Task;

                await _socketReader.ReadBytesIntoResponseHandler(bytesToRead, handler, _cancelSource.Token);
            }
        }
    }
}