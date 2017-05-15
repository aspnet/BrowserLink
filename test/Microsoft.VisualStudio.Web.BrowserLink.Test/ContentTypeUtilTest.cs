using System;
using System.Text;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    public class ContentTypeUtilTest
    {
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Uses native Windows methods")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Uses native Windows methods")]
        public void IsHtml_DetectsTextHtmlInContentType()
        {
            string[] htmlContentTypes = new string[]
            {
                "text/html",
                "TEXT/hTmL",
                "text/html; charset=utf-8",
                "text/html; any parameter",
            };

            string[] nonHtmlContentTypes = new string[]
            {
                "text/htmlx",
                "text/html charset=utf-8",
                null,
                "",
                "text/htm",
            };

            foreach (string htmlContentType in htmlContentTypes)
            {
                Assert.True(ContentTypeUtil.IsHtml(htmlContentType), htmlContentType);
            }

            foreach (string nonHtmlContentType in nonHtmlContentTypes)
            {
                Assert.False(ContentTypeUtil.IsHtml(nonHtmlContentType), nonHtmlContentType);
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Uses native Windows methods")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Uses native Windows methods")]
        public void IsHtml_DetectsHtmlInBuffer()
        {
            // Arrange
            byte[] buffer = Encoding.ASCII.GetBytes("<html><head></head><body></body></html>");

            // Act
            bool result = ContentTypeUtil.IsHtml("default.html",  buffer, 0, buffer.Length);

            // Assert
            Assert.True(result);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Uses native Windows methods")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Uses native Windows methods")]
        public void IsHtml_DetectsNonHtmlInBuffer()
        {
            // Arrange
            byte[] buffer = Encoding.ASCII.GetBytes("var j = 10;");

            // Act
            bool result = ContentTypeUtil.IsHtml("default.html", buffer, 0, buffer.Length);

            // Assert
            Assert.False(result);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Uses native Windows methods")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Uses native Windows methods")]
        public void IsHtml_DetectsHtmlInBufferWithOffset()
        {
            // Arrange
            byte[] buffer = Encoding.ASCII.GetBytes("<html><head></head><body></body></html>");
            byte[] bufferWithLeadingBytes = new byte[100 + buffer.Length];

            Array.Copy(buffer, 0, bufferWithLeadingBytes, 100, buffer.Length);

            // Act
            bool result = ContentTypeUtil.IsHtml("default.html", bufferWithLeadingBytes, 100, buffer.Length);

            // Assert
            Assert.True(result);
        }
    }
}
