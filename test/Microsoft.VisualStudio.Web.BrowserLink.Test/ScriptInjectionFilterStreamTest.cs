using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    public class ScriptInjectionFilterStreamTest
    {
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Uses native Windows methods")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Uses native Windows methods")]
        public void ScriptInjectionFilterStream_PassesHtmlToFilter()
        {
            // Arrange
            MockSocketAdapter serverSocket = new MockSocketAdapter();
            serverSocket.SendString("HTTP/1.1 200 OK\r\n", Encoding.ASCII);

            MockScriptInjectionFilterContext filterContext = new MockScriptInjectionFilterContext();

            ScriptInjectionFilterStream filterStream = CreateFilterStream(serverSocket, filterContext);

            byte[] bytesToSend = Encoding.UTF8.GetBytes("<html><head></head><body></body></html>");

            // Act
            filterStream.Write(bytesToSend, 0, bytesToSend.Length);

            // Assert
            Assert.Contains("\r\nContent-Type: text/html\r\n", serverSocket.SentContent);
            Assert.Contains("<html><head></head><body></body></html>", serverSocket.SentContent);
            AssertWithMessage.Equal(0, filterContext.GetResponseBody(Encoding.UTF8).Length, "No content should be sent to the response yet.");
            Assert.False(serverSocket.IsClosed, "Server connection should not have been closed.");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Uses native Windows methods")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Uses native Windows methods")]
        public void ScriptInjectionFilterStream_PassesJavascriptDirectlyToOutput()
        {
            // Arrange
            MockSocketAdapter serverSocket = new MockSocketAdapter();

            MockScriptInjectionFilterContext filterContext = new MockScriptInjectionFilterContext();

            ScriptInjectionFilterStream filterStream = CreateFilterStream(serverSocket, filterContext);

            byte[] bytesToSend = Encoding.UTF8.GetBytes("var i = 1234;");

            // Act
            filterStream.Write(bytesToSend, 0, bytesToSend.Length);

            // Assert
            AssertWithMessage.Equal(0, serverSocket.SentContent.Length, "Non-HTML content shouldn't be sent to the filter.");
            Assert.True(serverSocket.IsClosed, "Connection to VS was not closed when non-HTML content was detected.");
            Assert.Equal("var i = 1234;", filterContext.GetResponseBody(Encoding.UTF8));
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Uses native Windows methods")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Uses native Windows methods")]
        public void ScriptInjectionFilterStream_PassesCssDirectlyToOutput()
        {
            // Arrange
            MockSocketAdapter serverSocket = new MockSocketAdapter();
            MockScriptInjectionFilterContext filterContext = new MockScriptInjectionFilterContext();

            ScriptInjectionFilterStream filterStream = CreateFilterStream(serverSocket, filterContext);

            byte[] bytesToSend = Encoding.UTF8.GetBytes("body { font-weight: bold; }");

            // Act
            filterStream.Write(bytesToSend, 0, bytesToSend.Length);

            // Assert
            AssertWithMessage.Equal(0, serverSocket.SentContent.Length, "Non-HTML content shouldn't be sent to the filter.");
            Assert.True(serverSocket.IsClosed, "Connection to VS was not closed when non-HTML content was detected.");
            Assert.Equal("body { font-weight: bold; }", filterContext.GetResponseBody(Encoding.UTF8));
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Uses native Windows methods")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Uses native Windows methods")]
        public void ScriptInjectionFilterStream_ChecksResponseContentType()
        {
            // Arrange
            MockSocketAdapter serverSocket = new MockSocketAdapter();
            serverSocket.SendString("HTTP/1.1 200 OK\r\n", Encoding.ASCII);

            MockScriptInjectionFilterContext filterContext = new MockScriptInjectionFilterContext(contentType: "text/javascript");

            ScriptInjectionFilterStream filterStream = CreateFilterStream(serverSocket, filterContext);

            byte[] bytesToSend = Encoding.UTF8.GetBytes("var myhtml = \"<html></html>\";");

            // Act
            filterStream.Write(bytesToSend, 0, bytesToSend.Length);

            // Assert
            AssertWithMessage.Equal(0, serverSocket.SentContent.Length, "Non-HTML content shouldn't be sent to the filter.");
            Assert.True(serverSocket.IsClosed, "Connection to VS was not closed when non-HTML content was detected.");
            Assert.Equal("var myhtml = \"<html></html>\";", filterContext.GetResponseBody(Encoding.UTF8));
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Uses native Windows methods")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Uses native Windows methods")]
        public void ScriptInjectionFilterStream_CompleteFilterProcess()
        {
            // Arrange
            MockSocketAdapter serverSocket = new MockSocketAdapter();
            MockScriptInjectionFilterContext filterContext = new MockScriptInjectionFilterContext();

            ScriptInjectionFilterStream filterStream = CreateFilterStream(serverSocket, filterContext);

            byte[] bytesToSend = Encoding.UTF8.GetBytes("<html><head></head><body></body></html>");

            // Act I - Write some bytes
            Task writeTask = filterStream.WriteAsync(bytesToSend, 0, 20);

            // Assert
            TaskAssert.NotCompleted(writeTask, "Write task should not complete until a successful response comes from the server.");

            // Act Ia - Response from the server
            serverSocket.SendString("HTTP/1.1 200 OK\r\n", Encoding.ASCII);

            // Assert
            TaskAssert.Completed(writeTask, "The write task should complete after we receive the response line from the server.");
            Assert.Contains("<html><head></head><", serverSocket.SentContent);
            AssertWithMessage.Equal(0, filterContext.GetResponseBody(Encoding.UTF8).Length, "No content should be sent to the response yet.");
            Assert.False(serverSocket.IsClosed, "Server connection should not have been closed.");

            // Act II - Write some more bytes
            filterStream.Write(bytesToSend, 20, bytesToSend.Length - 20);

            // Assert
            Assert.Contains("body></body></html>", serverSocket.SentContent);
            AssertWithMessage.Equal(0, filterContext.GetResponseBody(Encoding.UTF8).Length, "No content should be sent to the response yet.");
            Assert.False(serverSocket.IsClosed, "Server connection should not have been closed.");

            // Act III - Wait for complete
            Task completeTask = filterStream.FlushAsync();

            // Assert
            TaskAssert.NotCompleted(completeTask, "Before response from server.");

            // Act IV - Send response from server
            serverSocket.SendString("Content-Length: 17\r\n\r\nXFiltered content", Encoding.ASCII);

            // Assert
            Assert.Equal("Filtered content", filterContext.GetResponseBody(Encoding.ASCII));
            TaskAssert.Completed(completeTask, "After response from server.");
            Assert.Equal(false, filterStream.ScriptInjectionTimedOut);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Uses native Windows methods")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Uses native Windows methods")]
        public void ScriptInjectionFilterStream_CompletePassthroughProcess()
        {
            // Arrange
            MockSocketAdapter serverSocket = new MockSocketAdapter();
            MockScriptInjectionFilterContext filterContext = new MockScriptInjectionFilterContext(contentType: "text/css");

            ScriptInjectionFilterStream filterStream = CreateFilterStream(serverSocket, filterContext);

            byte[] bytesToSend = Encoding.UTF8.GetBytes("body { font-size: xxlarge; } p { background-color: red }");

            // Act I - Write some bytes
            filterStream.Write(bytesToSend, 0, 20);

            // Assert
            Assert.Equal(filterContext.GetResponseBody(Encoding.UTF8), "body { font-size: xx");
            Assert.True(serverSocket.IsClosed, "Server connection should be closed.");

            // Act II - Write some more bytes
            filterStream.Write(bytesToSend, 20, bytesToSend.Length - 20);

            // Assert
            Assert.Equal(filterContext.GetResponseBody(Encoding.UTF8), "body { font-size: xxlarge; } p { background-color: red }");

            // Act III - Wait for complete
            Task completeTask = filterStream.FlushAsync();

            // Assert
            TaskAssert.Completed(completeTask, "Flush should complete immediately, because the filter is not being used.");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Uses native Windows methods")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Uses native Windows methods")]
        public void ScriptInjectionFilterStream_ErrorResponseCodeFromServer()
        {
            // Arrange
            MockSocketAdapter serverSocket = new MockSocketAdapter();
            MockScriptInjectionFilterContext filterContext = new MockScriptInjectionFilterContext();

            ScriptInjectionFilterStream filterStream = CreateFilterStream(serverSocket, filterContext);

            byte[] bytesToSend = Encoding.UTF8.GetBytes("<html><head></head><body></body></html>");

            // Act - Attempt to write some bytes
            Task writeTask = filterStream.WriteAsync(bytesToSend, 0, 20);

            // Assert
            TaskAssert.NotCompleted(writeTask, "Task should not complete until the response line is read.");

            // Act - Write an error response
            serverSocket.SendString("HTTP/1.1 404 Not Found\r\n", Encoding.ASCII);

            // Assert
            TaskAssert.Completed(writeTask, "Task should complete after the response line is received.");
            Assert.Equal("<html><head></head><", filterContext.GetResponseBody(Encoding.UTF8));
            Assert.True(serverSocket.IsClosed, "Server connection should be closed.");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Uses native Windows methods")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Uses native Windows methods")]
        public void ScriptInjectionFilterStream_ExceptionOnFirstWrite()
        {
            // Arrange
            MockSocketAdapter serverSocket = new MockSocketAdapter();
            MockScriptInjectionFilterContext filterContext = new MockScriptInjectionFilterContext();

            ScriptInjectionFilterStream filterStream = CreateFilterStream(serverSocket, filterContext);

            byte[] bytesToSend = Encoding.UTF8.GetBytes("<html><head></head><body></body></html>");

            // Act - Attempt to write some bytes
            serverSocket.ThrowExceptionOnNextSendAsync();

            Task writeTask = filterStream.WriteAsync(bytesToSend, 0, 20);

            // Assert
            TaskAssert.NotFaulted(writeTask, "Task should not be faulted. Filter should switch to passthrough mode.");
            Assert.Equal("<html><head></head><", filterContext.GetResponseBody(Encoding.UTF8));
            Assert.True(serverSocket.IsClosed, "Server connection should be closed.");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Uses native Windows methods")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Uses native Windows methods")]
        public void ScriptInjectionFilterStream_ExceptionOnSubsequentWrite()
        {
            // Arrange
            MockSocketAdapter serverSocket = new MockSocketAdapter();
            serverSocket.SendString("HTTP/1.1 200 OK\r\n", Encoding.ASCII);

            MockScriptInjectionFilterContext filterContext = new MockScriptInjectionFilterContext();

            ScriptInjectionFilterStream filterStream = CreateFilterStream(serverSocket, filterContext);

            byte[] bytesToSend = Encoding.UTF8.GetBytes("<html><head></head><body></body></html>");

            // Act I - Write some bytes
            filterStream.Write(bytesToSend, 0, 20);

            // Assert
            Assert.Contains("<html><head></head><", serverSocket.SentContent);
            AssertWithMessage.Equal(0, filterContext.GetResponseBody(Encoding.UTF8).Length, "No content should be sent to the response yet.");
            Assert.False(serverSocket.IsClosed, "Server connection should not have been closed.");

            // Act II - Attempt to write more bytes
            serverSocket.ThrowExceptionOnNextSendAsync();


            Task result = filterStream.WriteAsync(bytesToSend, 20, bytesToSend.Length - 20);

            // Assert
            TaskAssert.Faulted(result, "Exception should have been re-thrown");
            Assert.Equal("SendAsync after ThrowExceptionOnNextSendAsync was called.", result.Exception.InnerException.Message);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Uses native Windows methods")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Uses native Windows methods")]
        public void ScriptInjectionFilterStream_BecomesPassthroughOnTimeout()
        {
            // Arrange
            MockSocketAdapter serverSocket = new MockSocketAdapter();
            MockScriptInjectionFilterContext filterContext = new MockScriptInjectionFilterContext();

            ScriptInjectionFilterStream filterStream = CreateFilterStream(serverSocket, filterContext);

            byte[] bytesToSend = Encoding.UTF8.GetBytes("<html><head></head><body></body></html>");

            // Act I - Send some bytes
            Task writeTask = filterStream.WriteAsync(bytesToSend, 0, 20);

            // Assert
            TaskAssert.NotCompleted(writeTask, "Should be waiting for server to respond");

            // Act II - Wait for the request to time out
            System.Threading.Thread.Sleep(1100);

            // Assert
            TaskAssert.Completed(writeTask, "Write should complete when server fails to respond");
            Assert.True(serverSocket.IsClosed, "Should become passthrough");
            Assert.Equal("<html><head></head><", filterContext.GetResponseBody(Encoding.UTF8));
            AssertWithMessage.Equal(true, filterStream.ScriptInjectionTimedOut, "ScriptInjectionTimedOut");
        }

        private ScriptInjectionFilterStream CreateFilterStream(MockSocketAdapter socket, IScriptInjectionFilterContext context)
        {
            HttpSocketAdapter httpSocket = new HttpSocketAdapter("GET", new Uri("http://localhost:1234/abcd/injectScript"), socket);

            return new ScriptInjectionFilterStream(httpSocket, context);
        }
    }
}
