using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    public class DelayConnectingHttpSocketAdapterTest
    {
        private MockHttpSocketAdapter _createdAdapter = null;
        private string _responseContent = String.Empty;

        [Fact]
        public void DelayConnectingHttpSocketAdapter_CompleteRequest_DoesNotConnectIfNoDataAndNoResponse()
        {
            // Arrange
            IHttpSocketAdapter delayAdapter = new DelayConnectingHttpSocketAdapter(DoNotConnect);

            // Act
            Task result = delayAdapter.CompleteRequest();

            // Assert
            TaskAssert.Completed(result, "CompleteRequest should complete immediately when there is no request.");
        }

        [Fact]
        public void DelayConnectingHttpSocketAdapter_CompleteRequest_CompletesIfThereIsAResponseHandler()
        {
            // Arrange
            IHttpSocketAdapter delayAdapter = new DelayConnectingHttpSocketAdapter(ConnectOnlyOnce);

            delayAdapter.SetResponseHandler(HandleAsciiResponse);

            // Act
            Task result = delayAdapter.CompleteRequest();

            // Assert
            Assert.True(_createdAdapter != null, "Adapter should have been created");
            Assert.True(_createdAdapter.IsCompleted, "Adapter should be completed");
        }

        [Fact]
        public void DelayConnectingHttpSocketAdapter_CompleteRequest_DoesNotRecreateConnection()
        {
            // Arrange
            IHttpSocketAdapter delayAdapter = new DelayConnectingHttpSocketAdapter(ConnectOnlyOnce);

            byte[] bytesToWrite = Encoding.ASCII.GetBytes("Hello");
            delayAdapter.WriteToRequestAsync(bytesToWrite, 0, bytesToWrite.Length);

            // Act
            delayAdapter.CompleteRequest();

            // Assert
            Assert.True(_createdAdapter != null, "Adapter was not created.");
            Assert.True(_createdAdapter.IsCompleted, "Adapter was not Completed.");
        }

        [Fact]
        public void DelayConnectingHttpSocketAdapter_WriteToRequestAsync_ConnectsAndWritesData()
        {
            // Arrange
            IHttpSocketAdapter delayAdapter = new DelayConnectingHttpSocketAdapter(ConnectOnlyOnce);

            byte[] bytesToWrite = Encoding.ASCII.GetBytes("Hello, world!");

            // Act
            Task result = delayAdapter.WriteToRequestAsync(bytesToWrite, 7, 5);

            // Assert
            Assert.True(result != null, "No Task was returned.");
            Assert.True(_createdAdapter != null, "Adapter was not created.");
            Assert.Equal("world", _createdAdapter.RequestContent);
        }

        [Fact]
        public void DelayConnectingHttpSocketAdapter_WriteToRequestAsync_DoesNothingAfterFailureToConnect()
        {
            // Arrange
            IHttpSocketAdapter delayAdapter = new DelayConnectingHttpSocketAdapter(FailToConnect);

            byte[] bytesToWrite = Encoding.ASCII.GetBytes("Hello, world!");

            // Act
            Task result = delayAdapter.WriteToRequestAsync(bytesToWrite, 7, 5);

            // Assert
            TaskAssert.Completed(result);
        }

        [Fact]
        public void DelayConnectingHttpSocketAdapter_WriteToRequestAsync_DoesNotAttemptToConnectAfterFailure()
        {
            // Arrange
            IHttpSocketAdapter delayAdapter = new DelayConnectingHttpSocketAdapter(FailToConnect);

            byte[] bytesToWrite = Encoding.ASCII.GetBytes("Hello, world!");
            delayAdapter.WriteToRequestAsync(bytesToWrite, 7, 5);

            // Act
            Task result = delayAdapter.WriteToRequestAsync(bytesToWrite, 7, 5);

            // Assert
            //  If we got here, a second connection was not attempted
            TaskAssert.Completed(result);
        }

        [Fact]
        public void DelayConnectingHttpSocketAdapter_WriteToRequestAsync_WritesDataToSameConnection()
        {
            // Arrange
            IHttpSocketAdapter delayAdapter = new DelayConnectingHttpSocketAdapter(ConnectOnlyOnce);

            byte[] bytesToWrite = Encoding.ASCII.GetBytes("Hello, world!");
            delayAdapter.WriteToRequestAsync(bytesToWrite, 0, 7);

            // Act
            delayAdapter.WriteToRequestAsync(bytesToWrite, 7, 5);

            // Assert
            Assert.True(_createdAdapter != null, "Adapter was not created.");
            Assert.Equal("Hello, world", _createdAdapter.RequestContent);
        }

        [Fact]
        public void DelayConnectingHttpSocketAdapter_GetResponseStatusCode_ConnectsAndRequestsStatusCode()
        {
            // Arrange
            IHttpSocketAdapter delayAdapter = new DelayConnectingHttpSocketAdapter(ConnectOnlyOnce);

            // Act
            Task<int> result = delayAdapter.GetResponseStatusCode();

            // Assert
            Assert.True(_createdAdapter != null, "Adapter was not created.");
            TaskAssert.NotCompleted(result, "Status code should not be returned yet.");

            // Act
            _createdAdapter.SendResponseStatusCode(304);

            // Assert
            TaskAssert.ResultEquals(result, 304);
        }

        [Fact]
        public void DelayConnectingHttpSocketAdapter_GetResponseHeader_ConnectsAndGetsHeader()
        {
            // Arrange
            IHttpSocketAdapter delayAdapter = new DelayConnectingHttpSocketAdapter(ConnectOnlyOnce);

            // Act
            Task<string> result = delayAdapter.GetResponseHeader("Content-Length");

            // Assert
            Assert.True(_createdAdapter != null, "Adapter was not created.");
            TaskAssert.NotCompleted(result, "Status code should not be returned yet.");

            // Act
            _createdAdapter.SendResponseHeader("Content-Length", "1234");

            // Assert
            TaskAssert.ResultEquals(result, "1234");
        }

        [Fact]
        public void DelayConnectingHttpSocketAdapter_WaitForResponseComplete_ConnectsAndWaits()
        {
            // Arrange
            IHttpSocketAdapter delayAdapter = new DelayConnectingHttpSocketAdapter(ConnectOnlyOnce);

            // Act
            Task result = delayAdapter.WaitForResponseComplete();

            // Assert
            Assert.True(_createdAdapter != null, "Adapter was not created.");
            TaskAssert.NotCompleted(result, "Status code should not be returned yet.");

            // Act
            _createdAdapter.SendResponseComplete();

            // Assert
            TaskAssert.Completed(result);
        }

        [Fact]
        public void DelayConnectingHttpSocketAdapter_AddRequestHeader_DoesNotConnect()
        {
            // Arrange
            IHttpSocketAdapter delayAdapter = new DelayConnectingHttpSocketAdapter(DoNotConnect);

            // Act
            delayAdapter.AddRequestHeader("Hello", "World");

            // Assert
            //   If we got here, the connection was not created
        }

        [Fact]
        public void DelayConnectingHttpSocketAdapter_AddRequestHeader_AddsHeadersAfterConnection()
        {
            // Arrange
            IHttpSocketAdapter delayAdapter = new DelayConnectingHttpSocketAdapter(ConnectOnlyOnce);

            delayAdapter.AddRequestHeader("Hello", "World");
            delayAdapter.AddRequestHeader("Hello2", "Planet");

            // Act
            delayAdapter.WaitForResponseComplete();

            // Assert
            Assert.True(_createdAdapter.RequestHeaders.ContainsKey("Hello"), "Header 'Hello' should be added.");
            Assert.Equal("World", _createdAdapter.RequestHeaders["Hello"]);
            Assert.True(_createdAdapter.RequestHeaders.ContainsKey("Hello2"), "Header 'Hello2' should be added.");
            Assert.Equal("Planet", _createdAdapter.RequestHeaders["Hello2"]);
        }

        [Fact]
        public void DelayConnectingHttpSocketAdapter_AddRequestHeader_AddsHeadersToExistingConnection()
        {
            // Arrange
            IHttpSocketAdapter delayAdapter = new DelayConnectingHttpSocketAdapter(ConnectOnlyOnce);

            delayAdapter.GetResponseStatusCode();

            // Act
            delayAdapter.AddRequestHeader("Hello", "World");

            // Assert
            Assert.True(_createdAdapter.RequestHeaders.ContainsKey("Hello"), "Header 'Hello' should be added.");
            Assert.Equal("World", _createdAdapter.RequestHeaders["Hello"]);
        }

        [Fact]
        public void DelayConnectingHttpSocketAdapter_SetResponseHandler_DoesNotConnect()
        {
            // Arrange
            IHttpSocketAdapter delayAdapter = new DelayConnectingHttpSocketAdapter(DoNotConnect);

            // Act
            delayAdapter.SetResponseHandler(HandleAsciiResponse);

            // Assert
            //   If we got here, the connection was not created
        }

        [Fact]
        public void DelayConnectingHttpSocketAdapter_SetResponseHandler_SetsHandlerOnConnection()
        {
            // Arrange
            IHttpSocketAdapter delayAdapter = new DelayConnectingHttpSocketAdapter(ConnectOnlyOnce);

            delayAdapter.SetResponseHandler(HandleAsciiResponse);

            // Act
            delayAdapter.WaitForResponseComplete();

            // Assert
            Assert.True(_createdAdapter.HasResponseHandler, "Response handler was not set.");

            // Act
            _createdAdapter.SendResponseBodyContent("This is a test", Encoding.ASCII);

            // Assert
            Assert.Equal("This is a test", _responseContent);
        }

        [Fact]
        public void DelayConnectingHttpSocketAdapter_SetResponseHandler_SetsHandlerOnExistingConnection()
        {
            // Arrange
            IHttpSocketAdapter delayAdapter = new DelayConnectingHttpSocketAdapter(ConnectOnlyOnce);

            delayAdapter.GetResponseStatusCode();

            // Act
            delayAdapter.SetResponseHandler(HandleAsciiResponse);

            // Assert
            Assert.True(_createdAdapter.HasResponseHandler, "Response handler was not set.");

            // Act
            _createdAdapter.SendResponseBodyContent("This is a test", Encoding.ASCII);

            // Assert
            Assert.Equal("This is a test", _responseContent);
        }

        [Fact]
        public void DelayConnectingHttpSocketAdapter_Dispose_DisposesExistingConnection()
        {
            // Arrange
            IHttpSocketAdapter delayAdapter = new DelayConnectingHttpSocketAdapter(ConnectOnlyOnce);

            delayAdapter.WaitForResponseComplete();

            // Act
            delayAdapter.Dispose();

            // Assert
            Assert.True(_createdAdapter.IsDisposed);
        }

        [Fact]
        public void DelayConnectingHttpSocketAdapter_Dispose_DoesNothingIfNotConnected()
        {
            // Arrange
            IHttpSocketAdapter delayAdapter = new DelayConnectingHttpSocketAdapter(ConnectOnlyOnce);

            // Act
            delayAdapter.Dispose();

            // Assert
            //   Should do nothing
        }

        [Fact]
        public void DelayConnectingHttpSocketAdapter_Dispose_DoesNotDisposeASecondTime()
        {
            // Arrange
            IHttpSocketAdapter delayAdapter = new DelayConnectingHttpSocketAdapter(ConnectOnlyOnce);

            delayAdapter.CompleteRequest();
            delayAdapter.Dispose();

            // Act
            delayAdapter.Dispose();

            // Assert
            //   If we got here, the adapter was not Disposed a second time
        }

        private Task<IHttpSocketAdapter> ConnectOnlyOnce()
        {
            Assert.True(_createdAdapter == null, "A second connection is being created.");

            return Task.FromResult<IHttpSocketAdapter>(_createdAdapter = new MockHttpSocketAdapter());
        }

        private static Task<IHttpSocketAdapter> DoNotConnect()
        {
            Assert.True(false, "IHttpSocketAdapter should not be created.");

            return null;
        }

        private Task<IHttpSocketAdapter> FailToConnect()
        {
            Assert.True(_createdAdapter == null, "Attempted to create an adapter after a previous attempt failed.");

            _createdAdapter = new MockHttpSocketAdapter();

            return Task.FromResult((IHttpSocketAdapter)null);
        }

        private Task HandleAsciiResponse(byte[] buffer, int offset, int count)
        {
            _responseContent += Encoding.ASCII.GetString(buffer, offset, count);

            return StaticTaskResult.True;
        }
    }
}
