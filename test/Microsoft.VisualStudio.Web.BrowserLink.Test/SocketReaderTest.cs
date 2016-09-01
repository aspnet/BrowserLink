using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    public class SocketReaderTest
    {
        private readonly CancellationToken _dontCancel = new CancellationTokenSource().Token;

        [Fact]
        public void SocketReader_ReadChar_ReadsOneCharacter()
        {
            // Arrange
            MockSocketAdapter socket = new MockSocketAdapter();
            socket.SendString("ab", Encoding.Unicode);

            SocketReader reader = new SocketReader(socket);
            reader.SetEncoding(Encoding.Unicode);

            // Act
            Task<char> result = reader.ReadChar(_dontCancel);

            // Assert
            TaskAssert.ResultEquals(result, 'a');
        }

        [Fact]
        public void SocketReader_ReadChar_WaitsForDataReadingOneCharacter()
        {
            // Arrange
            MockSocketAdapter socket = new MockSocketAdapter();

            SocketReader reader = new SocketReader(socket);
            reader.SetEncoding(Encoding.Unicode);

            // Act
            Task<char> result = reader.ReadChar(_dontCancel);

            // Assert
            TaskAssert.NotCompleted(result, "Before sending data");

            // Act
            socket.SendString("ab", Encoding.Unicode);

            // Assert
            TaskAssert.ResultEquals(result, 'a', "After sending data");
        }

        [Fact]
        public void SocketReader_ReadChar_DoesNotWaitForDataReadingSecondCharacter()
        {
            // Arrange
            MockSocketAdapter socket = new MockSocketAdapter();
            socket.SendString("ab", Encoding.UTF8);

            SocketReader reader = new SocketReader(socket);
            reader.SetEncoding(Encoding.UTF8);

            Task<char> result1 = reader.ReadChar(_dontCancel);

            TaskAssert.ResultEquals(result1, 'a', "SocketReader did not read the first character in the buffer.");

            // Act
            Task<char> result2 = reader.ReadChar(_dontCancel);

            // Assert
            TaskAssert.ResultEquals(result2, 'b');
        }

        [Fact]
        public void SocketReader_ReadChar_KeepsWaitingIfNotEnoughDataIsReturned()
        {
            // Arrange
            byte[] sendBuffer = Encoding.UTF32.GetBytes("ΨΣ"); // Greek Psi, Sigma

            MockSocketAdapter socket = new MockSocketAdapter();

            SocketReader reader = new SocketReader(socket);
            reader.SetEncoding(Encoding.UTF32);

            // Act
            Task<char> result = reader.ReadChar(_dontCancel); // Try to read first character

            // Assert
            TaskAssert.NotCompleted(result, "Before sending any bytes");

            // Act
            socket.SendBytes(sendBuffer, 0, 1); // Send 1 byte

            // Assert
            TaskAssert.NotCompleted(result, "After sending 1 byte");

            // Act
            socket.SendBytes(sendBuffer, 1, 2); // Send 2 more bytes

            // Assert
            TaskAssert.NotCompleted(result, "After sending 2 more bytes");

            // Act
            socket.SendBytes(sendBuffer, 3, 2); // Send 2 more bytes

            // Assert
            TaskAssert.ResultEquals(result, 'Ψ', "After sending all bytes of first character.");

            // Act
            result = reader.ReadChar(_dontCancel); // Try to read second character

            // Assert
            TaskAssert.NotCompleted(result, "Before sending remaining bytes");

            // Act
            socket.SendBytes(sendBuffer, 5, 3); // Send remaining 3 bytes

            // Assert
            TaskAssert.ResultEquals(result, 'Σ', "After sending remaining bytes");
        }

        [Fact]
        public void SocketReader_ReadChar_TestByteByByteWithManyEncodings()
        {
            // Arrange
            Encoding[] encodings = new Encoding[]
            {
                Encoding.UTF8,
                Encoding.Unicode,
                Encoding.BigEndianUnicode,
                Encoding.UTF32
            };

            foreach (Encoding encoding in encodings)
            {
                MockSocketAdapter socket = new MockSocketAdapter();
                
                SocketReader reader = new SocketReader(socket);
                reader.SetEncoding(encoding);

                byte[] sendBuffer = encoding.GetBytes("ΨΣ");
                Task<char>[] results = new Task<char>[2];

                int currentResult = -1;

                for (int i = 0; i < sendBuffer.Length; i++)
                {
                    if (currentResult < 0 || results[currentResult].IsCompleted)
                    {
                        currentResult++;
                        results[currentResult] = reader.ReadChar(_dontCancel);
                    }

                    socket.SendBytes(sendBuffer, i, 1);
                }

                TaskAssert.ResultEquals(results[0], 'Ψ', "result[0] with encoding {0}", encoding.EncodingName);
                TaskAssert.ResultEquals(results[1], 'Σ', "result[1] with encoding {0}", encoding.EncodingName);
            }
        }

        [Fact]
        public void SocketReader_ReadChar_ReturnsZeroIfCancelled()
        {
            // Arrange
            MockSocketAdapter socket = new MockSocketAdapter();

            CancellationTokenSource cts = new CancellationTokenSource();

            SocketReader reader = new SocketReader(socket);
            Task<char> task = reader.ReadChar(cts.Token);

            // Act
            cts.Cancel();

            // Assert
            TaskAssert.ResultEquals(task, '\0');
        }

        [Fact]
        public void SocketReader_ReadLine_ReadsOneLine()
        {
            // Arrange
            MockSocketAdapter socket = new MockSocketAdapter();
            socket.SendString("This is a test.\r\nThis is left out.\r\n", Encoding.ASCII);

            SocketReader reader = new SocketReader(socket);

            // Act
            Task<string> result = reader.ReadLine(_dontCancel);

            // Assert
            TaskAssert.ResultEquals(result, "This is a test.");
        }

        [Fact]
        public void SocketReader_ReadLine_WaitsForDataReadingOneLine()
        {
            // Arrange
            MockSocketAdapter socket = new MockSocketAdapter();

            SocketReader reader = new SocketReader(socket);

            // Act
            Task<string> result = reader.ReadLine(_dontCancel);

            // Assert
            TaskAssert.NotCompleted(result, "Before sending data.");

            // Act
            socket.SendString("This is a test.\r\n", Encoding.ASCII);

            // Assert
            TaskAssert.ResultEquals(result, "This is a test.", "After sending data.");
        }

        [Fact]
        public void SocketReader_ReadLine_WaitsForMoreDataWhenLineIncomplete()
        {
            // Arrange
            MockSocketAdapter socket = new MockSocketAdapter();

            SocketReader reader = new SocketReader(socket);

            // Act
            Task<string> result = reader.ReadLine(_dontCancel);

            // Assert
            TaskAssert.NotCompleted(result, "Before sending data.");

            // Act
            socket.SendString("This is the first part of the string. ", Encoding.ASCII);

            // Assert
            TaskAssert.NotCompleted(result, "After sending first sentence.");

            // Act
            socket.SendString("This is the second part of the string.\r", Encoding.ASCII);

            // Assert
            TaskAssert.NotCompleted(result, "After sending second sentence.");

            // Act
            socket.SendString("\nThis is an extra, third sentence.\r\n", Encoding.ASCII);

            // Assert
            TaskAssert.ResultEquals(result, "This is the first part of the string. This is the second part of the string.", "After sending data.");
        }

        [Fact]
        public void SocketReader_ReadLine_IgnoresUnmatchedCarriageReturn()
        {
            // Arrange
            MockSocketAdapter socket = new MockSocketAdapter();
            socket.SendString("This is a test of an unmatched \r\r\n", Encoding.ASCII);

            SocketReader reader = new SocketReader(socket);

            // Act
            Task<string> result = reader.ReadLine(_dontCancel);

            // Assert
            TaskAssert.ResultEquals(result, "This is a test of an unmatched \r");
        }

        [Fact]
        public void SocketReader_ReadLine_IgnoresUnmatchedLineFeed()
        {
            // Arrange
            MockSocketAdapter socket = new MockSocketAdapter();
            socket.SendString("This is a test\rof an unmatched \n\r\n", Encoding.ASCII);

            SocketReader reader = new SocketReader(socket);

            // Act
            Task<string> result = reader.ReadLine(_dontCancel);

            // Assert
            TaskAssert.ResultEquals(result, "This is a test\rof an unmatched \n");
        }

        [Fact]
        public void SocketReader_ReadLine_ReadsLineLongerThanBuffer()
        {
            // Arrange
            string lineToSend = new String('Σ', count: 20000);

            MockSocketAdapter socket = new MockSocketAdapter();
            socket.SendString(lineToSend + "\r\n", Encoding.Unicode);

            SocketReader reader = new SocketReader(socket);
            reader.SetEncoding(Encoding.Unicode);

            // Act
            Task<string> result = reader.ReadLine(_dontCancel);

            // Assert
            TaskAssert.ResultEquals(result, lineToSend);
        }

        [Fact]
        public void SocketReader_ReadLine_ReturnsEmptyStringWhenCancelled()
        {
            // Arrange
            MockSocketAdapter socket = new MockSocketAdapter();

            CancellationTokenSource cts = new CancellationTokenSource();

            SocketReader reader = new SocketReader(socket);
            Task<string> lineResult = reader.ReadLine(cts.Token);

            // Act
            cts.Cancel();

            // Assert
            TaskAssert.ResultEquals(lineResult, String.Empty, "Task should return empty string when canceled.");
        }

        [Fact]
        public void SocketReader_ReadLine_ReturnsEmptyStringWhenAlreadyCancelled()
        {
            // Arrange
            MockSocketAdapter socket = new MockSocketAdapter();

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();

            SocketReader reader = new SocketReader(socket);

            // Act
            Task<string> lineResult = reader.ReadLine(cts.Token);

            // Assert
            TaskAssert.ResultEquals(lineResult, String.Empty, "Task should return empty string when canceled.");
        }

        [Fact]
        public void SocketReader_ReadBytesIntoResponseHandler_ReadsBytes()
        {
            // Arrange
            MockSocketAdapter socket = new MockSocketAdapter();
            socket.SendString("This is a test. This sentence isn't read.", Encoding.UTF8);

            MockResponseHandler handler = new MockResponseHandler(Encoding.UTF8);

            SocketReader reader = new SocketReader(socket);

            // Act
            Task<int> result = reader.ReadBytesIntoResponseHandler(15, handler.HandlerMethod, _dontCancel);

            // Assert
            TaskAssert.ResultEquals(result, 15);
            Assert.Equal("This is a test.", handler.Response);
        }

        [Fact]
        public void SocketReader_ReadBytesIntoResponseHandler_WaitsForData()
        {
            // Arrange
            MockSocketAdapter socket = new MockSocketAdapter();

            MockResponseHandler handler = new MockResponseHandler(Encoding.Unicode);

            SocketReader reader = new SocketReader(socket);

            // Act
            Task<int> result = reader.ReadBytesIntoResponseHandler(30, handler.HandlerMethod, _dontCancel);

            // Assert
            TaskAssert.NotCompleted(result, "Before any data sent");

            // Act
            socket.SendString("This is ", Encoding.Unicode);

            // Assert
            TaskAssert.NotCompleted(result, "After some data sent");

            // Act
            socket.SendString("a test. And then some.", Encoding.Unicode);

            // Assert
            TaskAssert.ResultEquals(result, 30, "After all data sent");
            Assert.Equal("This is a test.", handler.Response);
        }

        [Fact]
        public void SocketReader_ReadBytesIntoResponseHandler_ReadsMoreBytes()
        {
            // Arrange
            MockSocketAdapter socket = new MockSocketAdapter();
            socket.SendString("This is a test. This is the next sentence.", Encoding.ASCII);

            MockResponseHandler handler = new MockResponseHandler(Encoding.ASCII);

            SocketReader reader = new SocketReader(socket);

            // Act
            Task<int> result = reader.ReadBytesIntoResponseHandler(15, handler.HandlerMethod, _dontCancel);

            // Assert
            TaskAssert.ResultEquals(result, 15);
            Assert.Equal("This is a test.", handler.Response);

            // Act
            result = reader.ReadBytesIntoResponseHandler(27, handler.HandlerMethod, _dontCancel);

            // Assert
            TaskAssert.ResultEquals(result, 27);
            Assert.Equal("This is a test. This is the next sentence.", handler.Response);
        }

        [Fact]
        public void SocketReader_ReadBytesIntoResponseHandler_WaitsForHandlerToComplete()
        {
            // Arrange
            MockSocketAdapter socket = new MockSocketAdapter();
            socket.SendString("This is a test. This sentence isn't read.", Encoding.UTF8);

            MockResponseHandler handler = new MockResponseHandler(Encoding.UTF8);
            handler.Block = true;

            SocketReader reader = new SocketReader(socket);

            // Act
            Task<int> result = reader.ReadBytesIntoResponseHandler(15, handler.HandlerMethod, _dontCancel);

            // Assert
            TaskAssert.NotCompleted(result, "Before handler is unblocked.");

            // Act
            handler.Block = false;

            // Assert
            TaskAssert.ResultEquals(result, 15, "After handler is unblocked.");
            Assert.Equal("This is a test.", handler.Response);
        }

        [Fact]
        public void SocketReader_ReadBytesIntoResponseHandler_ReadMoreBytesThanBuffer()
        {
            // Arrange
            string stringToSend = new String('Σ', count: 20000);
            byte[] bufferToSend = Encoding.UTF32.GetBytes(stringToSend);

            MockSocketAdapter socket = new MockSocketAdapter();
            socket.SendBytes(bufferToSend, 0, bufferToSend.Length);

            MockResponseHandler handler = new MockResponseHandler(Encoding.UTF32);

            SocketReader reader = new SocketReader(socket);

            // Act
            Task<int> result = reader.ReadBytesIntoResponseHandler(bufferToSend.Length, handler.HandlerMethod, _dontCancel);

            // Assert
            TaskAssert.ResultEquals(result, bufferToSend.Length);
            Assert.Equal(stringToSend, handler.Response);
        }

        [Fact]
        public void SocketReader_ReadBytesIntoResponseHandler_ContinuesWaitingForBytes()
        {
            // Arrange
            string stringToSend = "123456789";
            byte[] bufferToSend = Encoding.ASCII.GetBytes(stringToSend);

            MockSocketAdapter socket = new MockSocketAdapter();
            socket.SendBytes(bufferToSend, 0, bufferToSend.Length);

            MockResponseHandler handler = new MockResponseHandler(Encoding.ASCII);

            SocketReader reader = new SocketReader(socket);

            // Act
            Task<int> task = reader.ReadBytesIntoResponseHandler(bufferToSend.Length + 1, handler.HandlerMethod, _dontCancel);

            // Assert
            TaskAssert.NotCompleted(task);
        }

        [Fact]
        public void SocketReader_ReadBytesIntoResponseHandler_StopsReadingBytesIfCancelled()
        {
            // Arrange
            string stringToSend = "123456789";
            byte[] bufferToSend = Encoding.ASCII.GetBytes(stringToSend);

            MockSocketAdapter socket = new MockSocketAdapter();
            socket.SendBytes(bufferToSend, 0, bufferToSend.Length);

            MockResponseHandler handler = new MockResponseHandler(Encoding.ASCII);

            CancellationTokenSource cts = new CancellationTokenSource();

            SocketReader reader = new SocketReader(socket);
            Task<int> task = reader.ReadBytesIntoResponseHandler(bufferToSend.Length + 1, handler.HandlerMethod, cts.Token);

            // Act
            cts.Cancel();

            // Assert
            TaskAssert.ResultEquals(task, bufferToSend.Length);
            Assert.Equal(stringToSend, handler.Response);
        }
    }
}
