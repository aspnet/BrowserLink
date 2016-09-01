// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    internal static class TaskHelpers
    {
        /// <summary>
        /// Add a timeout to a task. If the timeout expires before the underlying
        /// task completes, the wrapper task returns a fixed value.
        /// </summary>
        /// <param name="task">The task being awaited</param>
        /// <param name="timeout">The amount of time to wait</param>
        /// <param name="resultIfTimedOut">The result returned if the timeout expires</param>
        /// <remarks>
        /// The <paramref name="task"/> will continue to execute after the 
        /// <paramref name="timeout"/> expires, but tasks awaiting the wrapper will
        /// be unblocked.
        /// </remarks>
        public static async Task<ResultType> WaitWithTimeout<ResultType>(Task<ResultType> task, TimeSpan timeout, ResultType resultIfTimedOut)
        {
            CancellationTokenSource cancelTimeout = new CancellationTokenSource();

            Task<ResultType> timeoutTask = Task.Delay(timeout, cancelTimeout.Token).ContinueWith(x => resultIfTimedOut);

            Task<ResultType> completedTask = await Task.WhenAny(task, timeoutTask);

            cancelTimeout.Cancel();

            return completedTask.Result;
        }

        /// <summary>
        /// Wrap a task so that it can be cancelled. If the task is cancelled before
        /// the underlying task completes, the wrapper task returns a fixed value.
        /// </summary>
        /// <param name="task">The task being awaited</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <param name="resultIfCancelled">The result returned if the timeout expires</param>
        /// <returns>A task that can be cancelled before it completes</returns>
        /// <remarks>
        /// The <paramref name="task"/> will continue to execute after the
        /// <paramref name="cancellationToken"/> is set, but tasks awaiting the
        /// wrapper will be unblocked.
        /// </remarks>
        public static async Task<ResultType> WaitWithCancellation<ResultType>(Task<ResultType> task, CancellationToken cancellationToken, ResultType resultIfCancelled)
        {
            TaskCompletionSource<ResultType> cancelTcs = new TaskCompletionSource<ResultType>();

            cancellationToken.Register(delegate ()
            {
                cancelTcs.TrySetResult(resultIfCancelled);
            });

            return await await Task.WhenAny(task, cancelTcs.Task);
        }
    }
}
