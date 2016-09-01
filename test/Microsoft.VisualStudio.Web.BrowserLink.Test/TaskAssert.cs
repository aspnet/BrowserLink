using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    internal static class TaskAssert
    {
        public static void ResultEquals<ResultType>(Task<ResultType> task, ResultType expected, string messageFormat = null, params object[] args)
        {
            TaskAssert.NotFaulted(task, messageFormat, args);
            TaskAssert.Completed(task, messageFormat, args);

            if (task.Result == null)
            {
                if (expected == null)
                {
                    return;
                }
            }
            else if (task.Result.Equals(expected))
            {
                return;
            }

            ThrowFailure(
                GetMethodFailureMessage(nameof(ResultEquals)),
                FormatMessage("Expected: <{0}>. Actual: <{1}>.", SafeParam(expected), SafeParam(task.Result)),
                FormatMessage(messageFormat, args));
        }

        public static void Faulted(Task task, string messageFormat = null, params object[] args)
        {
            if (!task.IsFaulted)
            {
                TaskAssert.ThrowFailure(
                    GetMethodFailureMessage(nameof(Faulted)),
                    FormatMessage(messageFormat, args));
            }
        }

        public static void NotFaulted(Task task, string messageFormat = null, params object[] args)
        {
            if (task.IsFaulted)
            {
                ThrowFailure(
                    task.Exception,
                    GetMethodFailureMessage(nameof(NotFaulted)),
                    FormatMessage(task.Exception), 
                    FormatMessage(messageFormat, args));
            }
        }

        public static void Completed(Task task, string messageFormat = null, params object[] args)
        {
            TaskAssert.NotFaulted(task, messageFormat, args);

            if (!task.IsCompleted)
            {
                ThrowFailure(
                    GetMethodFailureMessage(nameof(Completed)),
                    FormatMessage(messageFormat, args));
            }
        }

        public static void NotCompleted(Task task, string messageFormat = null, params object[] args)
        {
            TaskAssert.NotFaulted(task, messageFormat, args);

            if (task.IsCompleted)
            {
                ThrowFailure(
                    GetMethodFailureMessage(nameof(NotCompleted)),
                    FormatMessage(messageFormat, args));
            }
        }

        private static string FormatMessage(Exception exception)
        {
            exception = UnwrapException(exception);

            // TODO: Add this condition back with the right exception type
            //if (exception is AssertFailedException)
            //{
            //    return null;
            //}
            //else
            //{
                return String.Format("{0}: {1}.", exception.GetType().Name, exception.Message);
            //}
        }

        private static string FormatMessage(string messageFormat, params object[] messageArgs)
        {
            if (messageArgs == null || messageArgs.Length == 0)
            {
                return messageFormat;
            }
            else
            {
                return String.Format(messageFormat, messageArgs);
            }
        }

        private static void ThrowFailure(params string[] failureMessages)
        {
            ThrowFailure(null, failureMessages);
        }

        private static void ThrowFailure(Exception exception, params string[] failureMessages)
        {
            string failureMessage = String.Join(" ", failureMessages.Where(x => !String.IsNullOrWhiteSpace(x)));

            if (exception != null)
            {
                exception = UnwrapException(exception);

                // TODO: Add this code back with the right exception type
                //if (exception is AssertFailedException)
                //{
                //    throw exception;
                //}

                //throw new AssertFailedException(failureMessage, exception);
            }
            else
            {
                //throw new AssertFailedException(failureMessage);
            }
        }

        private static Exception UnwrapException(Exception exception)
        {
            while (exception is AggregateException)
            {
                exception = ((AggregateException)exception).InnerExceptions[0];
            }

            return exception;
        }

        private static string GetMethodFailureMessage(string methodName)
        {
            return String.Format("{0}.{1} failed.", nameof(TaskAssert), methodName);
        }

        private static object SafeParam(object param)
        {
            if (param == null)
            {
                return "(null)";
            }
            else
            {
                return param;
            }
        }
    }
}
