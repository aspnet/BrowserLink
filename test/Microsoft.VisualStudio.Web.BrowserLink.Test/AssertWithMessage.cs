using Xunit;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    /// <summary>
    /// These wrappers accept useful messages about Assert failures, but I'm
    /// not sure how to output them to Xunit.
    /// </summary>
    internal static class AssertWithMessage
    {
        public static void Equal(string expected, string actual, string messageFormat, params object[] messageArgs)
        {
            Assert.Equal(expected, actual);
        }

        public static void Equal(int expected, int actual, string messageFormat, params object[] messageArgs)
        {
            Assert.Equal(expected, actual);
        }

        public static void Equal(bool expected, bool actual, string messageFormat, params object[] messageArgs)
        {
            Assert.Equal(expected, actual);
        }

        public static void Null(object @object, string messageFormat, params object[] messageArgs)
        {
            Assert.Null(@object);
        }

        public static void NotNull(object @object, string messageFormat, params object[] messageArgs)
        {
            Assert.NotNull(@object);
        }
    }
}
