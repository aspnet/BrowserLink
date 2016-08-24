using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    internal class MockHttpSocketAdapter : IHttpSocketAdapter
    {
        private Dictionary<string, TaskCompletionSource<string>> _responseHeaderRequests = new Dictionary<string, TaskCompletionSource<string>>();
        private TaskCompletionSource<int> _statusCodeTask = new TaskCompletionSource<int>();
        private TaskCompletionSource<object> _responseCompleteTask = new TaskCompletionSource<object>();

        public Dictionary<string, string> RequestHeaders = new Dictionary<string, string>();
        private ResponseHandler _responseHandler;

        private StringBuilder _requestContent = new StringBuilder();

        public void SendResponseStatusCode(int statusCode)
        {
            TaskAssert.NotCompleted(_statusCodeTask.Task, "MockHttpSocketAdapter: Response Status Code can only be set once.");

            _statusCodeTask.SetResult(statusCode);
        }

        public void SendResponseHeader(string name, string value)
        {
            TaskCompletionSource<string> tcs;

            if (!_responseHeaderRequests.TryGetValue(name, out tcs))
            {
                tcs = new TaskCompletionSource<string>();
                _responseHeaderRequests[name] = tcs;
            }

            TaskAssert.NotCompleted(tcs.Task, "MockHttpSocketAdapter: Response header '{0}' can only be set once.", name);

            tcs.SetResult(value);
        }

        public void SendResponseBodyContent(string content, Encoding encoding)
        {
            AssertWithMessage.NotNull(_responseHandler, "No response handler was set.");

            byte[] bytes = encoding.GetBytes(content);

            _responseHandler.Invoke(bytes, 0, bytes.Length);
        }

        public void SendResponseComplete()
        {
            TaskAssert.NotCompleted(_responseCompleteTask.Task, "MockHttpSocketAdapter: Response Complete can only be sent once.");

            _responseCompleteTask.SetResult(true);
        }

        public bool IsDisposed { get; private set; }
        public bool IsCompleted { get; private set; }
        public string RequestContent { get { return _requestContent.ToString(); } }
        public bool HasResponseHandler { get { return _responseHandler != null; } }


        void IHttpSocketAdapter.AddRequestHeader(string name, string value)
        {
            RequestHeaders.Add(name, value);
        }

        Task IHttpSocketAdapter.CompleteRequest()
        {
            IsCompleted = true;

            return StaticTaskResult.True;
        }

        void IDisposable.Dispose()
        {
            Assert.False(IsDisposed, "Calling Dispose on an adapter that was already disposed");

            IsDisposed = true;
        }

        Task<string> IHttpSocketAdapter.GetResponseHeader(string headerName)
        {
            TaskCompletionSource<string> tcs;

            if (!_responseHeaderRequests.TryGetValue(headerName, out tcs))
            {
                tcs = new TaskCompletionSource<string>();
                _responseHeaderRequests[headerName] = tcs;
            }

            return tcs.Task;
        }

        Task<int> IHttpSocketAdapter.GetResponseStatusCode()
        {
            return _statusCodeTask.Task;
        }

        void IHttpSocketAdapter.SetResponseHandler(ResponseHandler handler)
        {
            AssertWithMessage.Null(_responseHandler, "SetResponseHandler was called more than once.");
            AssertWithMessage.NotNull(handler, "SetResponseHandler was called with a null handler.");

            _responseHandler = handler;
        }

        Task IHttpSocketAdapter.WaitForResponseComplete()
        {
            return _responseCompleteTask.Task;
        }

        Task IHttpSocketAdapter.WriteToRequestAsync(byte[] buffer, int offset, int count)
        {
            _requestContent.Append(Encoding.ASCII.GetString(buffer, offset, count));

            return StaticTaskResult.True;
        }
    }
}
