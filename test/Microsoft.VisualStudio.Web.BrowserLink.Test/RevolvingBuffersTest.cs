using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    public class RevolvingBuffersTest
    {
        [Fact]
        public void RevolvingBuffers_WriteAndReadOneBuffer()
        {
            // Arrange
            char[] data = "Hello, world!".ToArray();

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(20);

            // Act
            buffers.CopyDataToBuffer(data, 0, data.Length);
            ArraySegment<char> result = buffers.GetBufferedData();

            // Assert
            Assert.Equal("Hello, world!", GetString(result));
        }

        [Fact]
        public void RevolvingBuffers_BufferedDataDoesNotChangeWhenInputBufferChanges()
        {
            // Arrange
            char[] data = "Hello, world!".ToArray();

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(20);

            buffers.CopyDataToBuffer(data, 0, data.Length);
            ArraySegment<char> result = buffers.GetBufferedData();

            // Act
            data[2] = '!';

            // Assert
            Assert.Equal("Hello, world!", GetString(result));
        }

        [Fact]
        public void RevolvingBuffers_TwoWritesAndOneRead()
        {
            // Arrange
            char[] data = "Hello, world!".ToArray();

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(20);

            // Act
            buffers.CopyDataToBuffer(data, 0, 6);
            buffers.CopyDataToBuffer(data, 6, 6);

            // Assert
            ArraySegment<char> result = buffers.GetBufferedData();

            Assert.Equal("Hello, world", GetString(result));
        }

        [Fact]
        public void RevolvingBuffers_WriteAndReadAfterRead()
        {
            char[] data = "Hello, world!".ToArray();

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(20);

            buffers.CopyDataToBuffer(data, 0, 6);
            buffers.GetBufferedData();

            // Act
            buffers.CopyDataToBuffer(data, 6, 6);

            // Assert
            ArraySegment<char> result = buffers.GetBufferedData();

            Assert.Equal(" world", GetString(result));
        }

        [Fact]
        public void RevolvingBuffers_WriteAfterReadDoesntModifyBuffer()
        {
            char[] data = "Hello, world!".ToArray();

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(20);

            buffers.CopyDataToBuffer(data, 0, 6);
            ArraySegment<char> result = buffers.GetBufferedData();

            // Act
            buffers.CopyDataToBuffer(data, 6, 6);

            // Assert
            Assert.Equal("Hello,", GetString(result));
        }

        [Fact]
        public void RevolvingBuffers_WritesTooMuchDataToBuffer()
        {
            // Arrange
            char[] data = "Hello, world!".ToArray();

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(10);

            // Act
            buffers.CopyDataToBuffer(data, 0, data.Length);

            // Assert
            ArraySegment<char> result = buffers.GetBufferedData();

            Assert.Equal("Hello, world!", GetString(result));
        }

        [Fact]
        public void RevolvingBuffers_WritesTooMuchDataToBufferInTwoWrites()
        {
            // Arrange
            char[] data = "Hello, world!".ToArray();

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(10);
            buffers.CopyDataToBuffer(data, 0, 6);

            // Act
            buffers.CopyDataToBuffer(data, 6, 6);

            // Assert
            ArraySegment<char> result = buffers.GetBufferedData();
            Assert.Equal("Hello,", GetString(result));

            result = buffers.GetBufferedData();
            Assert.Equal(" world", GetString(result));
        }

        [Fact]
        public void RevolvingBuffers_WritesMoreDataIntoTheNextBuffer()
        {
            // Arrange
            char[] data = "Hello, world!".ToArray();

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(10);
            buffers.CopyDataToBuffer(data, 0, 6);
            buffers.CopyDataToBuffer(data, 6, 6);

            // Act
            buffers.CopyDataToBuffer(data, 12, 1);

            // Assert
            ArraySegment<char> result = buffers.GetBufferedData();
            Assert.Equal("Hello,", GetString(result));

            result = buffers.GetBufferedData();
            Assert.Equal(" world!", GetString(result));
        }

        [Fact]
        public void RevolvingBuffers_ReadsWithNoDataInBuffer()
        {
            // Arrange
            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(10);

            // Act
            ArraySegment<char> result = buffers.GetBufferedData();

            // Assert
            Assert.Equal("", GetString(result));
        }

        [Fact]
        public void RevolvingBuffers_ReadsAfterAllDataIsRead()
        {
            // Arrange
            char[] data = "Hello, world!".ToArray();

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(20);

            buffers.CopyDataToBuffer(data, 0, data.Length);
            buffers.GetBufferedData();

            // Act
            ArraySegment<char> result = buffers.GetBufferedData();

            // Assert
            Assert.Equal("", GetString(result));
        }

        [Fact]
        public void RevolvingBuffers_WritesIntoManyBuffers()
        {
            // Arrange
            char[] data = "Hello, world!".ToArray();

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(10);

            // Act
            buffers.CopyDataToBuffer(data, 0, 6);
            buffers.CopyDataToBuffer(data, 6, 6);
            buffers.CopyDataToBuffer(data, 12, 1);
            buffers.CopyDataToBuffer(data, 0, 6);

            // Assert
            Assert.Equal("Hello,", GetString(buffers.GetBufferedData()));
            Assert.Equal(" world!", GetString(buffers.GetBufferedData()));
            Assert.Equal("Hello,", GetString(buffers.GetBufferedData()));
            Assert.Equal("", GetString(buffers.GetBufferedData()));
        }

        [Fact]
        public void RevolvingBuffers_ReusesAvailableBuffers()
        {
            // Arrange
            char[] data = "Hello, world!".ToArray();

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(10);

            buffers.CopyDataToBuffer(data, 0, 6); // Write to buffer 1
            buffers.CopyDataToBuffer(data, 6, 6); // Write to buffer 2

            buffers.GetBufferedData(); // Read from buffer 1
            buffers.GetBufferedData(); // Read from buffer 2; buffer 1 is now available

            // Act
            buffers.CopyDataToBuffer(data, 7, 5); // Write to buffer 1

            // Assert
#if DEBUG
            AssertWithMessage.Equal(2, buffers.BufferCount, "BufferCount");
#endif
            Assert.Equal("world", GetString(buffers.GetBufferedData()));
        }

        [Fact]
        public void RevolvingBuffers_WritesToLargeEnoughBufferWhenASmallerBufferIsAvailable()
        {
            // Arrange
            char[] data = "Hello there, world! How are you doing today?".ToArray();

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(10);

            buffers.CopyDataToBuffer(data, 0, 6); // Write to small buffer 1
            buffers.CopyDataToBuffer(data, 6, 6); // Write to small buffer 2
            buffers.CopyDataToBuffer(data, 12, 6); // Write to small buffer 3

            buffers.GetBufferedData(); // Read from buffer 1
            buffers.GetBufferedData(); // Read from buffer 2; buffer 1 is now available
            buffers.GetBufferedData(); // Read from buffer 3; buffers 1 and 2 are now available

            buffers.CopyDataToBuffer(data, 3, 6); // Write to small buffer 1

            // Act
            buffers.CopyDataToBuffer(data, 20, 24);

            // Assert
            Assert.Equal("lo the", GetString(buffers.GetBufferedData()));
            Assert.Equal("How are you doing today?", GetString(buffers.GetBufferedData()));
        }

        [Fact]
        public void RevolvingBuffers_GetBufferedDataAsync_ReturnsBufferedData()
        {
            // Arrange
            char[] data = "Hello, world!".ToArray();

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(20);

            buffers.CopyDataToBuffer(data, 0, data.Length);

            // Act
            Task<ArraySegment<char>> task = buffers.GetBufferedDataAsync();

            // Assert
            TaskAssert.Completed(task, "Task should be completed immediately.");
            Assert.Equal("Hello, world!", GetString(task.Result));
        }

        [Fact]
        public void RevolvingBuffers_GetBufferedDataAsync_WaitsForBufferedData()
        {
            // Arrange
            char[] data = "Hello, world!".ToArray();

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(20);

            // Act
            Task<ArraySegment<char>> task = buffers.GetBufferedDataAsync();

            // Assert
            TaskAssert.NotCompleted(task, "Task should not be completed until data is buffered.");

            // Act
            buffers.CopyDataToBuffer(data, 0, data.Length);

            // Assert
            TaskAssert.Completed(task, "Task should be completed after data is buffered.");
            Assert.Equal("Hello, world!", GetString(task.Result));
        }

        [Fact]
        public void RevolvingBuffers_GetBufferedDataAsync_ReturnsEmptyBufferWhenDisposed()
        {
            // Arrange
            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(20);

            // Act
            Task<ArraySegment<char>> task = buffers.GetBufferedDataAsync();

            // Assert
            TaskAssert.NotCompleted(task, "Task should not be completed until data is buffered.");

            // Act
            buffers.Dispose();

            // Assert
            TaskAssert.Completed(task, "Task should be completed after buffers are disposed.");
            Assert.Equal("", GetString(task.Result));
        }

        [Fact]
        public void RevolvingBuffers_WaitForBufferEmptyAsync_ReturnsImmediatelyIfNoData()
        {
            // Arrange
            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(20);

            // Act
            Task task = buffers.WaitForBufferEmptyAsync();

            // Assert
            TaskAssert.Completed(task, "Task should be completed immediately.");
        }

        [Fact]
        public void RevolvingBuffers_WaitForBufferEmptyAsync_ReturnsImmediatelyIfAllDataHasBeenRead()
        {
            // Arrange
            char[] data = "Hello, world!".ToArray();

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(20);

            buffers.CopyDataToBuffer(data, 0, 3);
            buffers.GetBufferedData(); // Read the buffer
            buffers.GetBufferedData(); // Release the buffer and get an empty buffer

            // Act
            Task task = buffers.WaitForBufferEmptyAsync();

            // Assert
            TaskAssert.Completed(task, "Task should be completed immediately.");
        }

        [Fact]
        public void RevolvingBuffers_WaitForBufferEmptyAsync_WaitsAfterDataHasBeenWritten()
        {
            // Arrange
            char[] data = "Hello, world!".ToArray();

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(20);

            buffers.CopyDataToBuffer(data, 0, 2);

            // Act
            Task task = buffers.WaitForBufferEmptyAsync();

            // Assert
            TaskAssert.NotCompleted(task, "Task should not be completed until data is read.");
        }

        [Fact]
        public void RevolvingBuffers_WaitForBufferEmptyAsync_WaitsWhileBufferIsRead()
        {
            // Arrange
            char[] data = "Hello, world!".ToArray();

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(20);

            buffers.CopyDataToBuffer(data, 0, 2);
            Task task = buffers.WaitForBufferEmptyAsync();

            // Act
            buffers.GetBufferedData(); // Read the buffer

            // Assert
            TaskAssert.NotCompleted(task, "Task should not be completed until buffer is released.");
        }

        [Fact]
        public void RevolvingBuffers_WaitForBufferEmptyAsync_WaitsWhileThereIsStillMoreDataToRead()
        {
            // Arrange
            char[] data = "Hello, world!".ToArray();

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(20);

            buffers.CopyDataToBuffer(data, 0, data.Length);
            buffers.GetBufferedData(); // Read the buffer
            buffers.CopyDataToBuffer(data, 0, data.Length);
            Task task = buffers.WaitForBufferEmptyAsync();

            // Act
            buffers.GetBufferedData(); // Release the buffer, read the next buffer

            // Assert
            TaskAssert.NotCompleted(task, "Task should not be completed while there is still more data.");
        }

        [Fact]
        public void RevolvingBuffers_WaitForBufferEmptyAsync_WaitsWhileThereIsStillMoreDataToReadAsync()
        {
            // Arrange
            char[] data = "Hello, world!".ToArray();

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(20);

            buffers.CopyDataToBuffer(data, 0, data.Length);
            buffers.GetBufferedData(); // Read the buffer
            buffers.CopyDataToBuffer(data, 0, data.Length);
            Task task = buffers.WaitForBufferEmptyAsync();

            // Act
            buffers.GetBufferedDataAsync(); // Release the buffer, read the next buffer

            // Assert
            TaskAssert.NotCompleted(task, "Task should not be completed while there is still more data.");
        }

        [Fact]
        public void RevolvingBuffers_WaitForBufferEmptyAsync_ReturnsAfterBufferIsReleased()
        {
            // Arrange
            char[] data = "Hello, world!".ToArray();

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(20);

            buffers.CopyDataToBuffer(data, 0, 2);
            buffers.GetBufferedData(); // Read the buffer
            Task task = buffers.WaitForBufferEmptyAsync();

            // Act
            buffers.GetBufferedData(); // Release the buffer

            // Assert
            TaskAssert.Completed(task, "Task should be completed when buffer is released.");
        }

        [Fact]
        public void RevolvingBuffers_WaitForBufferEmptyAsync_ReturnsAfterBufferIsReleasedAsync()
        {
            // Arrange
            char[] data = "Hello, world!".ToArray();

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(20);

            buffers.CopyDataToBuffer(data, 0, 2);
            buffers.GetBufferedData(); // Read the buffer
            Task task = buffers.WaitForBufferEmptyAsync();

            // Act
            buffers.GetBufferedDataAsync(); // Release the buffer

            // Assert
            TaskAssert.Completed(task, "Task should be completed when buffer is released.");
        }

        [Fact]
        public void RevolvingBuffers_InlineAsyncDisposalInteraction()
        {
            // Arrange
            char[] data = "Hello, world!".ToArray();

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(20);

            // Reader waits for a chunk of data
            Task read1 = buffers.GetBufferedDataAsync();
            Assert.False(read1.IsCompleted, "read1.IsCompleted");

            // Writer writes some data
            buffers.CopyDataToBuffer(data, 0, data.Length);
            Assert.True(read1.IsCompleted, "read1.IsCompleted");

            // Writer is done writing data
            Task wait = buffers.WaitForBufferEmptyAsync();
            Assert.False(wait.IsCompleted, "All data isn't finished until the reader releases the last buffer");

            // Writer will dispose as soon as reading is complete
            wait.ContinueWith(task =>
            {
                buffers.Dispose();
            }, TaskContinuationOptions.ExecuteSynchronously);

            // Reader looks for more data
            Task<ArraySegment<char>> read2 = buffers.GetBufferedDataAsync();
            Assert.True(wait.IsCompleted, "Wait should be completed now");
            Assert.True(read2.IsCompleted, "Read should also return because buffers were disposed immediately");
            AssertWithMessage.Equal("", GetString(read2.Result), "read2.Result");
        }

        [Fact]
        public void RevolvingBuffers_Multithreaded()
        {
            // Arrange
            char[] data = "Hello, world!".ToArray();
            int iterations = 10000;
            StringBuilder result = new StringBuilder();

            string expected = String.Concat(Enumerable.Repeat("Hello, world!", iterations));

            RevolvingBuffers<char> buffers = new RevolvingBuffers<char>(2);

            Exception writingThreadException = null;
            Thread writingThread = new Thread(delegate ()
            {
                try
                {

                    for (int i = 0; i < iterations; i++)
                    {
                        for (int i2 = 0; i2 < data.Length; i2++)
                        {
                            buffers.CopyDataToBuffer(data, i2, 1);
                        }
                    }
                }
                catch (Exception ex)
                {
                    writingThreadException = ex;
                }
            });

            Exception readingThreadException = null;
            Thread readingThread = new Thread(delegate ()
            {
                try
                {
                    while (result.Length < data.Length * iterations)
                    {
                        result.Append(GetString(buffers.GetBufferedData()));
                    }
                }
                catch (Exception ex)
                {
                    readingThreadException = ex;
                }
            });

            // Act
            writingThread.Start();
            readingThread.Start();

            // Assert
            bool completed = readingThread.Join(millisecondsTimeout: 1000);

            AssertWithMessage.Null(writingThreadException, "Exception in writingThread: {0}", writingThreadException);
            AssertWithMessage.Null(readingThreadException, "Exception in readingThread: {0}", readingThreadException);
            Assert.True(completed, "Process did not complete.");

            AssertWithMessage.Equal(expected.Length, result.Length, "result.Length");
            Assert.Equal(expected, result.ToString());
        }

        private static string GetString(ArraySegment<char> arraySegment)
        {
            return new string(Enumerable.ToArray(arraySegment));
        }
    }
}
