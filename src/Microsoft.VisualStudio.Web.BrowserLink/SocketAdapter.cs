// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    /// <summary>
    /// SocketAdapter wraps a System.Net.Socket, and provides Task-based async
    /// methods for interacting with the Socket. It also provides an abstraction
    /// layer for unit tests, so that they don't have to work with real sockets.
    /// </summary>
    internal interface ISocketAdapter : IDisposable
    {
        Task<int> ReceiveAsync(byte[] buffer, int offset, int count);

        Task SendAsync(IList<ArraySegment<byte>> buffers);
    }

    internal class SocketAdapter : ISocketAdapter
    {
        private Socket _socket;
        private SocketAsyncEventArgs _sendEventArgs = new SocketAsyncEventArgs();
        private SocketAsyncEventArgs _receiveEventArgs = new SocketAsyncEventArgs();

        public SocketAdapter(Socket socket)
        {
            _socket = socket;

            _sendEventArgs.Completed += OnAsyncComplete;
            _receiveEventArgs.Completed += OnAsyncComplete;
        }

        internal static async Task<ISocketAdapter> OpenSocketAsync(Uri url)
        {
            try
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

                SocketAdapter adapter = new SocketAdapter(socket);

                await adapter.ConnectAsync(url);

                return adapter;
            }
            catch
            {
                // Handle any socket error and return null
            }

            return null;
        }

        public void Dispose()
        {
            if (_socket != null)
            {
                _socket.Dispose();
                _socket = null;
            }

            if (_sendEventArgs != null)
            {
                _sendEventArgs.Completed -= OnAsyncComplete;

                _sendEventArgs.Dispose();
                _sendEventArgs = null;
            }

            if (_receiveEventArgs != null)
            {
                _receiveEventArgs.Completed -= OnAsyncComplete;

                _receiveEventArgs.Dispose();
                _receiveEventArgs = null;
            }
        }

        private Task ConnectAsync(Uri url)
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

            try
            {
                _sendEventArgs.UserToken = tcs;
                _sendEventArgs.RemoteEndPoint = new DnsEndPoint(url.Host, url.Port);

                _socket.ConnectAsync(_sendEventArgs);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }

            return tcs.Task;
        }

        public Task<int> ReceiveAsync(byte[] buffer, int offset, int count)
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

            if (_receiveEventArgs.UserToken != null)
            {
                // UserToken is set with a TCS whenever a receive is in progress, and is
                // cleared as soon as the send is complete. Do not allow two receive
                // operations at the same time.
                tcs.SetException(new InvalidOperationException("Attempted to read data when a read was already in progress."));
            }
            else
            {
                try
                {
                    _receiveEventArgs.SetBuffer(buffer, offset, count);
                    _receiveEventArgs.UserToken = tcs;

                    _socket.ReceiveAsync(_receiveEventArgs);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }

            return tcs.Task;
        }

        public Task SendAsync(IList<ArraySegment<byte>> buffers)
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

            if (_sendEventArgs.UserToken != null)
            {
                // UserToken is set with a TCS whenever a send is in progress, and is
                // cleared as soon as the send is complete. Do not allow two send
                // operations at the same time.
                tcs.SetException(new InvalidOperationException("Attempted to send data when a send was already in progress."));
            }
            else
            {
                try
                {
                    if (buffers.Count > 0)
                    {
                        _sendEventArgs.BufferList = buffers;
                        _sendEventArgs.UserToken = tcs;

                        _socket.SendAsync(_sendEventArgs);
                    }
                    else
                    {
                        tcs.SetResult(0);
                    }
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }

            return tcs.Task;
        }

        private static void OnAsyncComplete(object sender, SocketAsyncEventArgs e)
        {
            TaskCompletionSource<int> tcs = e.UserToken as TaskCompletionSource<int>;

            // Clear this immediately! When the result is set on the TCS, it could
            // trigger more send or receive requests, and they will fail if it looks
            // like a request is still in progress.
            e.UserToken = null;

            try
            {
                if (e.SocketError == SocketError.OperationAborted)
                {
                    tcs.SetResult(0);
                }
                else if (e.SocketError != SocketError.Success)
                {
                    tcs.SetException(new SocketException((int)e.SocketError));
                }
                else
                {
                    tcs.SetResult(e.BytesTransferred);
                }
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }
    }
}