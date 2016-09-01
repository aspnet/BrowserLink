// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    /// <summary>
    /// Null-object implementation of a connection, which stands in for a connection
    /// that could not be created.
    /// </summary>
    internal class FailedConnectionHttpSocketAdapter : IHttpSocketAdapter
    {
        void IHttpSocketAdapter.AddRequestHeader(string name, string value)
        {
        }

        Task IHttpSocketAdapter.CompleteRequest()
        {
            return StaticTaskResult.Complete;
        }

        void IDisposable.Dispose()
        {
        }

        Task<string> IHttpSocketAdapter.GetResponseHeader(string headerName)
        {
            return StaticTaskResult.NullString;
        }

        Task<int> IHttpSocketAdapter.GetResponseStatusCode()
        {
            return StaticTaskResult.HttpInternalServerError;
        }

        void IHttpSocketAdapter.SetResponseHandler(ResponseHandler handler)
        {
        }

        Task IHttpSocketAdapter.WaitForResponseComplete()
        {
            return StaticTaskResult.Complete;
        }

        Task IHttpSocketAdapter.WriteToRequestAsync(byte[] buffer, int offset, int count)
        {
            return StaticTaskResult.Complete;
        }
    }
}