// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    /// <summary>
    /// This class wraps an IHttpSocketAdapter so that it does not connect
    /// until the last possible moment, and it does not connect unless absolutely
    /// necessary. This way, a connection can be initialized early, but there is
    /// no performance hit unless the connection is used.
    /// </summary>
    /// <remarks>
    /// Each method is implemented using one of two strategies:
    /// 1. If a method is really using the connection (sending or receiving data),
    ///    make sure the connection has been created and pass the call through.
    /// 2. If a method is just initializing the connection (e.g. setting headers),
    ///    pass the call through only if the connection already exists. Otherwise,
    ///    store the call arguments and replay them after the connection is created.
    /// </remarks>
    internal class DelayConnectingHttpSocketAdapter : IHttpSocketAdapter
    {
        private Task<IHttpSocketAdapter> _connectedSocketTask = null;
        private Func<Task<IHttpSocketAdapter>> _connectFunction;

        private List<KeyValuePair<string, string>> _headers = new List<KeyValuePair<string, string>>();
        private ResponseHandler _responseHandler = null;

        internal DelayConnectingHttpSocketAdapter(Func<Task<IHttpSocketAdapter>> connectFunction)
        {
            _connectFunction = connectFunction;
        }

        void IHttpSocketAdapter.AddRequestHeader(string name, string value)
        {
            if (_connectedSocketTask != null)
            {
                _connectedSocketTask.Result.AddRequestHeader(name, value);
            }
            else
            {
                _headers.Add(new KeyValuePair<string, string>(name, value));
            }
        }

        async Task IHttpSocketAdapter.CompleteRequest()
        {
            // If a connection hasn't been created yet, no data has been sent.
            // If there's no response listener, no data will be received.
            // If both of those are true, it's a safe bet that the connection is unnecessary.
            if (_connectedSocketTask == null && _responseHandler == null)
            {
                return;
            }
            else
            {
                IHttpSocketAdapter socket = await GetConnectedSocketAsync();

                await socket.CompleteRequest();
            }
        }

        void IDisposable.Dispose()
        {
            if (_connectedSocketTask != null)
            {
                _connectedSocketTask.Result.Dispose();
                _connectedSocketTask = null;
            }
        }

        async Task<string> IHttpSocketAdapter.GetResponseHeader(string headerName)
        {
            IHttpSocketAdapter socket = await GetConnectedSocketAsync();

            return await socket.GetResponseHeader(headerName);
        }

        async Task<int> IHttpSocketAdapter.GetResponseStatusCode()
        {
            IHttpSocketAdapter socket = await GetConnectedSocketAsync();

            return await socket.GetResponseStatusCode();
        }

        void IHttpSocketAdapter.SetResponseHandler(ResponseHandler handler)
        {
            if (_connectedSocketTask != null)
            {
                _connectedSocketTask.Result.SetResponseHandler(handler);
            }
            else
            {
                _responseHandler = handler;
            }
        }

        async Task IHttpSocketAdapter.WaitForResponseComplete()
        {
            IHttpSocketAdapter socket = await GetConnectedSocketAsync();

            await socket.WaitForResponseComplete();
        }

        async Task IHttpSocketAdapter.WriteToRequestAsync(byte[] buffer, int offset, int count)
        {
            IHttpSocketAdapter socket = await GetConnectedSocketAsync();

            await socket.WriteToRequestAsync(buffer, offset, count);
        }

        private Task<IHttpSocketAdapter> GetConnectedSocketAsync()
        {
            if (_connectedSocketTask == null)
            {
                _connectedSocketTask = CreateSocketConnectionAsync();
            }

            return _connectedSocketTask;
        }

        private async Task<IHttpSocketAdapter> CreateSocketConnectionAsync()
        {
            IHttpSocketAdapter socket = await _connectFunction.Invoke();

            if (socket == null)
            {
                // Failed to create the connection, so instead we create this null
                // object to handle future requests with no-ops.
                socket = new FailedConnectionHttpSocketAdapter();
            }
            else
            {
                if (_responseHandler != null)
                {
                    socket.SetResponseHandler(_responseHandler);
                }

                foreach (KeyValuePair<string, string> header in _headers)
                {
                    socket.AddRequestHeader(header.Key, header.Value);
                }
            }

            return socket;
        }
    }
}