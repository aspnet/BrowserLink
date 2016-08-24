using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    public class HttpSocketAdapterTest
    {
        [Fact]
        public void HttpSocketAdapter_WritesRequestLineAndHeaders()
        {
            // Arrange
            MockSocketAdapter serverSocket = new MockSocketAdapter();

            HttpSocketAdapter clientSocket = new HttpSocketAdapter("GET", new Uri("http://localhost:1234/Hello.aspx?Name=%22Value%22#Fragment"), serverSocket);

            // Act
            clientSocket.CompleteRequest();

            // Assert
            Assert.StartsWith("GET /Hello.aspx?Name=%22Value%22#Fragment HTTP/1.1\r\n", serverSocket.SentContent);
            Assert.Contains("Host: localhost:1234\r\n", serverSocket.SentContent);
            Assert.Contains("Transfer-Encoding: chunked\r\n", serverSocket.SentContent);
            Assert.EndsWith("\r\n\r\n0\r\n\r\n", serverSocket.SentContent);
        }

        [Fact]
        public void HttpSocketAdapter_AddsCustomHeaders()
        {
            // Arrange
            MockSocketAdapter serverSocket = new MockSocketAdapter();

            HttpSocketAdapter clientSocket = new HttpSocketAdapter("POST", new Uri("http://bing.com"), serverSocket);

            // Act
            clientSocket.AddRequestHeader("Content-Encoding", "UTF-16");
            clientSocket.CompleteRequest();

            // Assert
            Assert.StartsWith("POST / HTTP/1.1\r\n", serverSocket.SentContent);
            Assert.Contains("Host: bing.com:80\r\n", serverSocket.SentContent);
            Assert.Contains("Transfer-Encoding: chunked\r\n", serverSocket.SentContent);
            Assert.Contains("Content-Encoding: UTF-16\r\n", serverSocket.SentContent);
            Assert.EndsWith("\r\n\r\n0\r\n\r\n", serverSocket.SentContent);
        }

        [Fact]
        public void HttpSocketAdapter_WritesChunkedContent()
        {
            // Arrange
            MockSocketAdapter serverSocket = new MockSocketAdapter();

            HttpSocketAdapter clientSocket = new HttpSocketAdapter("POST", new Uri("http://bing.com"), serverSocket);

            byte[] buffer = Encoding.ASCII.GetBytes("First content, Second content.");

            // Act
            clientSocket.WriteToRequestAsync(buffer, 0, 14);

            // Assert
            Assert.EndsWith("\r\n\r\nE\r\nFirst content,\r\n", serverSocket.SentContent);

            // Act
            clientSocket.WriteToRequestAsync(buffer, 14, 16);

            // Assert
            Assert.EndsWith("\r\n\r\nE\r\nFirst content,\r\n10\r\n Second content.\r\n", serverSocket.SentContent);
        }

        [Fact]
        public void HttpSocketAdapter_GetResponseStatusCode_ReturnsStatusCode()
        {
            // Arrange
            MockSocketAdapter serverSocket = new MockSocketAdapter();

            HttpSocketAdapter clientSocket = new HttpSocketAdapter("GET", new Uri("http://bing.com"), serverSocket);

            // Act
            Task<int> result = clientSocket.GetResponseStatusCode();

            // Assert
            TaskAssert.NotCompleted(result, "Before status line was sent.");

            // Act
            serverSocket.SendString("HTTP/1.1 200 OK\r\n", Encoding.ASCII);

            // Assert
            TaskAssert.ResultEquals(result, 200, "After status line was sent.");
        }

        [Fact]
        public void HttpSocketAdapter_GetResponseStatusCode_MalformedStatusLine()
        {
            // Arrange
            MockSocketAdapter serverSocket = new MockSocketAdapter();
            serverSocket.SendString("Hello\r\n", Encoding.ASCII);

            HttpSocketAdapter clientSocket = new HttpSocketAdapter("GET", new Uri("http://bing.com"), serverSocket);

            // Act
            Task<int> result = clientSocket.GetResponseStatusCode();

            // Assert
            TaskAssert.ResultEquals(result, 500);
        }

        [Fact]
        public void HttpSocketAdapter_GetResponseStatusCode_SocketException()
        {
            // Arrange
            MockSocketAdapter serverSocket = new MockSocketAdapter();

            HttpSocketAdapter clientSocket = new HttpSocketAdapter("GET", new Uri("http://bing.com"), serverSocket);

            Task<int> responseCodeTask = clientSocket.GetResponseStatusCode();
            Task<string> responseHeaderTask = clientSocket.GetResponseHeader("test header");
            Task responseCompleteTask = clientSocket.WaitForResponseComplete();

            // Act
            serverSocket.ThrowExceptionFromReceiveAsync();

            // Assert
            TaskAssert.Faulted(responseCodeTask, "GetResponseStatusCode should have failed.");
            TaskAssert.Faulted(responseHeaderTask, "GetResponseHeader should have failed.");
            TaskAssert.Faulted(responseCompleteTask, "WaitForResponseComplete should have failed.");

            AssertWithMessage.Equal("An error occurred.", responseCodeTask.Exception.InnerException.Message, "Wrong exception for GetResponseStatusCode");
            AssertWithMessage.Equal("An error occurred.", responseHeaderTask.Exception.InnerException.Message, "Wrong exception for GetResponseHeader");
            AssertWithMessage.Equal("An error occurred.", responseCompleteTask.Exception.InnerException.Message, "Wrong exception for WaitForResponseComplete");
        }

        [Fact]
        public void HttpSocketAdapter_GetResponseStatusCode_MalformedStatusCode()
        {
            // Arrange
            MockSocketAdapter serverSocket = new MockSocketAdapter();
            serverSocket.SendString("HTTP/1.1 NOTANUMBER OK\r\n", Encoding.ASCII);

            HttpSocketAdapter clientSocket = new HttpSocketAdapter("GET", new Uri("http://bing.com"), serverSocket);

            // Act
            Task<int> result = clientSocket.GetResponseStatusCode();

            // Assert
            TaskAssert.ResultEquals(result, 500);
        }

        [Fact]
        public void HttpSocketAdapter_GetResponseHeader_ReturnsHeaderWhenAvailable()
        {
            // Arrange
            MockSocketAdapter serverSocket = new MockSocketAdapter();
            serverSocket.SendString("HTTP/1.1 200 OK\r\n", Encoding.ASCII);

            HttpSocketAdapter clientSocket = new HttpSocketAdapter("GET", new Uri("http://bing.com"), serverSocket);

            // Act
            Task<string> result = clientSocket.GetResponseHeader("content-encoding");

            // Assert
            TaskAssert.NotCompleted(result, "Before header sent");

            // Act
            serverSocket.SendString("Content-Encoding: UTF-16\r\n", Encoding.ASCII);

            // Assert
            TaskAssert.NotCompleted(result, "Header values should not be returned until all headers are read.");

            // Act - Blank newline means end of headers
            serverSocket.SendString("Content-Length: 45\r\n\r\n", Encoding.ASCII);

            // Assert
            TaskAssert.ResultEquals(result, "UTF-16");

            // Act
            Task<string> result2 = clientSocket.GetResponseHeader("content-length");

            // Assert
            TaskAssert.ResultEquals(result2, "45");

            // Act
            Task<string> result3 = clientSocket.GetResponseHeader("user-agent");

            // Assert
            TaskAssert.ResultEquals(result3, null);
        }

        [Fact]
        public void HttpSocketAdapter_GetResponseHeader_SocketException()
        {
            // Arrange
            MockSocketAdapter serverSocket = new MockSocketAdapter();
            serverSocket.SendString("HTTP/1.1 200 OK\r\n", Encoding.ASCII);

            HttpSocketAdapter clientSocket = new HttpSocketAdapter("GET", new Uri("http://bing.com"), serverSocket);

            Task<int> responseCodeTask = clientSocket.GetResponseStatusCode();
            Task<string> responseHeaderTask = clientSocket.GetResponseHeader("test header");
            Task responseCompleteTask = clientSocket.WaitForResponseComplete();

            TaskAssert.Completed(responseCodeTask, "GetResponseStatusCode should have failed.");
            AssertWithMessage.Equal(200, responseCodeTask.Result, "Wrong result for GetResponseStatusCode");

            // Act
            serverSocket.ThrowExceptionFromReceiveAsync();

            // Assert
            TaskAssert.Faulted(responseHeaderTask, "GetResponseHeader should have failed.");
            TaskAssert.Faulted(responseCompleteTask, "WaitForResponseComplete should have failed.");

            AssertWithMessage.Equal("An error occurred.", responseHeaderTask.Exception.InnerException.Message, "Wrong exception for GetResponseHeader");
            AssertWithMessage.Equal("An error occurred.", responseCompleteTask.Exception.InnerException.Message, "Wrong exception for WaitForResponseComplete");
        }

        [Fact]
        public void HttpSocketAdapter_ReadsContentIntoResponseHandler()
        {
            // Arrange
            MockSocketAdapter serverSocket = new MockSocketAdapter();
            serverSocket.SendString("HTTP/1.1 200 OK\r\n", Encoding.ASCII);
            serverSocket.SendString("Content-Length: 40\r\n\r\n", Encoding.ASCII);
            serverSocket.SendString("1234567890", Encoding.Unicode);

            HttpSocketAdapter clientSocket = new HttpSocketAdapter("GET", new Uri("http://bing.com"), serverSocket);

            MockResponseHandler handler = new MockResponseHandler(Encoding.Unicode);

            // Act
            clientSocket.SetResponseHandler(handler.HandlerMethod);

            // Assert
            Assert.Equal("1234567890", handler.Response);

            // Act
            Task responseComplete = clientSocket.WaitForResponseComplete();

            // Assert
            TaskAssert.NotCompleted(responseComplete, "After sending first chunk of data");

            // Act - Send too much data to complete Content-Length
            serverSocket.SendString("abcdefghijklmnopqrstuvwxyz", Encoding.Unicode);

            // Assert - Only Content-Length was read
            Assert.Equal("1234567890abcdefghij", handler.Response);
            TaskAssert.Completed(responseComplete, "After sending remaining data");
        }

        [Fact]
        public void HttpSocketAdapter_ReadsChunkedContentIntoResponseHandler()
        {
            // Arrange
            MockSocketAdapter serverSocket = new MockSocketAdapter();
            serverSocket.SendString("HTTP/1.1 200 OK\r\n", Encoding.ASCII);
            serverSocket.SendString("Transfer-Encoding: chunked\r\n\r\n", Encoding.ASCII);

            HttpSocketAdapter clientSocket = new HttpSocketAdapter("GET", new Uri("http://bing.com"), serverSocket);

            MockResponseHandler handler = new MockResponseHandler(Encoding.ASCII);

            // Act
            clientSocket.SetResponseHandler(handler.HandlerMethod);

            // Assert
            Assert.Equal("", handler.Response);

            // Act
            serverSocket.SendString("C\r\nHello, world\r\n", Encoding.ASCII);

            // Assert
            Assert.Equal("Hello, world", handler.Response);

            // Act
            Task responseComplete = clientSocket.WaitForResponseComplete();

            // Assert
            TaskAssert.NotCompleted(responseComplete, "After sending 'Hello, World'");

            // Act
            serverSocket.SendString("8\r\nwide", Encoding.ASCII);

            // Assert
            Assert.Equal("Hello, worldwide", handler.Response);
            TaskAssert.NotCompleted(responseComplete, "After sending 'wide'");

            // Act
            serverSocket.SendString("!!\r\n", Encoding.ASCII);

            // Assert
            Assert.Equal("Hello, worldwide!!\r\n", handler.Response);
            TaskAssert.NotCompleted(responseComplete, "after sending '!!\r\n'");

            // Act
            serverSocket.SendString("\r\n", Encoding.ASCII);

            // Assert
            Assert.Equal("Hello, worldwide!!\r\n", handler.Response);
            TaskAssert.NotCompleted(responseComplete, "after sending '\r\n'");

            // Act
            serverSocket.SendString("0\r\n", Encoding.ASCII);

            // Assert
            Assert.Equal("Hello, worldwide!!\r\n", handler.Response);
            TaskAssert.Completed(responseComplete, "after completing the response");
        }

        [Fact]
        public void HttpSocketAdapter_ThrowsExceptionForMalformedChunkedResponse()
        {
            // Arrange
            MockSocketAdapter serverSocket = new MockSocketAdapter();
            serverSocket.SendString("HTTP/1.1 200 OK\r\n", Encoding.ASCII);
            serverSocket.SendString("Transfer-Encoding: chunked\r\n\r\n", Encoding.ASCII);

            HttpSocketAdapter clientSocket = new HttpSocketAdapter("GET", new Uri("http://bing.com"), serverSocket);

            MockResponseHandler handler = new MockResponseHandler(Encoding.ASCII);
            clientSocket.SetResponseHandler(handler.HandlerMethod);

            // Act
            serverSocket.SendString("Hi!\r\n", Encoding.ASCII);

            // Assert
            TaskAssert.Faulted(clientSocket.WaitForResponseComplete(), "ResponseReader should throw an exception if the response is malformed. Otherwise, the response will be blank and there will be nothing pointing to why it happened.");
        }
    }
}
