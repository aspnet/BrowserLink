// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    /// <summary>
    /// This class wraps the output stream of an IHttpSocketAdapter into a Stream
    /// interface, for use with System.IO.*Writer classes.
    /// </summary>
    internal class HttpAdapterRequestStream : Stream
    {
        private RevolvingBuffers<byte> _buffers = new RevolvingBuffers<byte>(1024);

        internal HttpAdapterRequestStream(IHttpSocketAdapter adapter)
        {
            SendDataFromBuffersAsync(_buffers, adapter);
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
            var buffer = _buffers;
            if (buffer != null)
            {
                _buffers = null;

                buffer.WaitForBufferEmptyAsync().ContinueWith(task =>
                {
                    buffer.Dispose();
                });
            }
        }

        protected override void Dispose(bool disposing)
        {
            Flush();
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
            _buffers.CopyDataToBuffer(buffer, offset, count);
        }

        /// <remarks>
        /// This function is the pump that pushes data from the buffers into an
        /// HTTP connection. It asynchronously waits on data, then asynchronously
        /// waits while the data is sent, then waits on more data, etc.
        /// </remarks>
        private static async void SendDataFromBuffersAsync(RevolvingBuffers<byte> buffer, IHttpSocketAdapter adapter)
        {
            while (true)
            {
                ArraySegment<byte> bufferToSend = await buffer.GetBufferedDataAsync();
                if (bufferToSend.Count == 0)
                {
                    break;
                }

                await adapter.WriteToRequestAsync(bufferToSend.Array, bufferToSend.Offset, bufferToSend.Count);
            }

            await adapter.CompleteRequest();

            adapter.Dispose();
        }
    }
}