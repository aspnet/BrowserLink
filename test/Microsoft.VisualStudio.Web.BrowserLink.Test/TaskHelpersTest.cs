using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    public class TaskHelpersTest
    {
        [Fact]
        public void TaskHelpers_WaitWithTimeout_ReturnsCompletedTaskResult()
        {
            // Arrange
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            TimeSpan timeout = TimeSpan.FromMilliseconds(1000);

            tcs.SetResult("Hello");

            // Act
            Task<string> result = TaskHelpers.WaitWithTimeout(tcs.Task, timeout, null);

            // Assert
            TaskAssert.ResultEquals(result, "Hello");
        }

        [Fact]
        public void TaskHelpers_WaitWithTimeout_ReturnsTaskResultWhenComplete()
        {
            // Arrange
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            TimeSpan timeout = TimeSpan.FromMilliseconds(1000);

            Task<string> result = TaskHelpers.WaitWithTimeout(tcs.Task, timeout, null);

            // Act
            tcs.SetResult("Hello");

            // Assert
            TaskAssert.ResultEquals(result, "Hello");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "https://github.com/aspnet/BrowserLink/issues/43 ")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "https://github.com/aspnet/BrowserLink/issues/43")]
        public void TaskHelpers_WaitWithTimeout_ReturnsTimeoutResultIfTaskNotComplete()
        {
            // Arrange
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            TimeSpan timeout = TimeSpan.FromMilliseconds(500);

            Task<string> result = TaskHelpers.WaitWithTimeout(tcs.Task, timeout, "Timed out");

            // Act
            bool completed = result.Wait(millisecondsTimeout: 550);

            // Assert
            Assert.True(completed, "Task did not time out");
            TaskAssert.ResultEquals(result, "Timed out");
        }

        [Fact]
        public void TaskHelpers_WaitWithTimeout_HandlesTaskFault()
        {
            // Arrange
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            TimeSpan timeout = TimeSpan.FromMilliseconds(1000);

            Task<string> result = TaskHelpers.WaitWithTimeout(tcs.Task, timeout, "Timed out");

            // Act
            tcs.SetException(new Exception("MY TEST ERROR"));

            // Assert
            TaskAssert.Faulted(result);
        }

        [Fact]
        public void TaskHelpers_WaitWithCancellation_DoesNotCompleteUntilTaskCompletes()
        {
            // Arrange
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            CancellationTokenSource cts = new CancellationTokenSource();

            // Act
            Task<string> result = TaskHelpers.WaitWithCancellation(tcs.Task, cts.Token, null);

            // Assert
            TaskAssert.NotCompleted(result);
        }
        
        [Fact]
        public void TaskHelpers_WaitWithCancellation_ReturnsCompletedTask()
        {
            // Arrange
            Task<string> task = Task.FromResult("Hello");
            CancellationTokenSource cts = new CancellationTokenSource();

            // Act
            Task<string> result = TaskHelpers.WaitWithCancellation(task, cts.Token, null);

            // Assert
            TaskAssert.ResultEquals(result, "Hello");
        }

        [Fact]
        public void TaskHelpers_WaitWithCancellation_ReturnsTaskWhenCompleted()
        {
            // Arrange
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            CancellationTokenSource cts = new CancellationTokenSource();

            Task<string> result = TaskHelpers.WaitWithCancellation(tcs.Task, cts.Token, null);

            // Act
            tcs.SetResult("Hello");

            // Assert
            TaskAssert.ResultEquals(result, "Hello");
        }

        [Fact]
        public void TaskHelpers_WaitWithCancellation_ReturnsCancelValueForCancelledToken()
        {
            // Arrange
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            CancellationTokenSource cts = new CancellationTokenSource();

            cts.Cancel();

            // Act
            Task<string> result = TaskHelpers.WaitWithCancellation(tcs.Task, cts.Token, "Cancelled");

            // Assert
            TaskAssert.ResultEquals(result, "Cancelled");
        }

        [Fact]
        public void TaskHelpers_WaitWithCancellation_ReturnsCancelValueWhenTokenCancels()
        {
            // Arrange
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            CancellationTokenSource cts = new CancellationTokenSource();

            Task<string> result = TaskHelpers.WaitWithCancellation(tcs.Task, cts.Token, "Cancelled");

            // Act
            cts.Cancel();

            // Assert
            TaskAssert.ResultEquals(result, "Cancelled");
        }

        [Fact]
        public void TaskHelpers_WaitWithCancellation_HandlesMultipleCancels()
        {
            // Arrange
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            CancellationTokenSource cts = new CancellationTokenSource();

            Task<string> result = TaskHelpers.WaitWithCancellation(tcs.Task, cts.Token, "Cancelled");

            cts.Cancel();

            // Act
            cts.Cancel();

            // Assert
            TaskAssert.ResultEquals(result, "Cancelled");
        }

        [Fact]
        public void TaskHelpers_WaitWithCancellation_ResturnsTaskResultIfTaskCompleteAndCanceledSimultaneously()
        {
            // Arrange
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            CancellationTokenSource cts = new CancellationTokenSource();

            tcs.SetResult("Not Cancelled");
            cts.Cancel();

            // Act
            Task<string> result = TaskHelpers.WaitWithCancellation(tcs.Task, cts.Token, "Cancelled");

            // Assert
            TaskAssert.ResultEquals(result, "Not Cancelled");
        }
    }
}
