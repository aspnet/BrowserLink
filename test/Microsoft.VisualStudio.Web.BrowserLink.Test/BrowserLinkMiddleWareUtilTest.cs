using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.VisualStudio.Web.BrowserLink.Test
{
    public class BrowserLinkMiddleWareUtilTest
    {
        private string headerIfNoneMatch = "If-None-Match";
        private string ETag = "ETag";

        [Fact]
        public void GetCurrentPort_ContainsPortNumber()
        {
            // Arrange
            string connectionString = "http://localhost:3082/acskakdjaksa";

            // Act
            int currentPort = BrowserLinkMiddleWareUtil.GetCurrentPort(connectionString);

            // Assert
            Assert.Equal(currentPort, 3082);
        }

        [Fact]
        public void GetCurrentPort_EmptyConnectionString()
        {
            // Arrange
            string connectionString = "";

            // Act
            int currentPort = BrowserLinkMiddleWareUtil.GetCurrentPort(connectionString);

            // Assert
            Assert.Equal(currentPort, -1);
        }

        [Fact]
        public void GetRequestPort_ContainsPortNumber()
        {
            // Arrange
            IHeaderDictionary dict = new HeaderDictionary
            {
                { headerIfNoneMatch, new StringValues("\"1d20ac81ccb7b87:7576\"") }
            };

            // Act
            int port = BrowserLinkMiddleWareUtil.GetRequestPort(dict);

            // Assert
            Assert.Equal(port, 7576);
        }

        [Fact]
        public void GetRequestPort_EmptyIfNoneMatch()
        {
            // Arrange
            IHeaderDictionary dict = new HeaderDictionary
            {
                { headerIfNoneMatch, new StringValues("\"\"") }
            };

            // Act
            int port = BrowserLinkMiddleWareUtil.GetRequestPort(dict);

            // Assert
            Assert.Equal(port, -1);
        }

        [Fact]
        public void DeletePortFromEtag_ContainsPortNumber()
        {
            // Arrange
            IHeaderDictionary dict = new HeaderDictionary
            {
                { headerIfNoneMatch, new StringValues("\"1d20ac81ccb7b87:7576\"") }
            };
            string expectedOutput = "\"1d20ac81ccb7b87\"";

            // Act
            BrowserLinkMiddleWareUtil.DeletePortFromETag(dict);
            RequestHeaders requestHeader = new RequestHeaders(dict);
            string actualOutput = requestHeader.IfNoneMatch[0].ToString();

            // Assert
            Assert.Equal(actualOutput, expectedOutput);         
        }
        
        [Fact]
        public void AddToETag_NotNullETag()
        {
            // Arrange
            int port = 7576;
            IHeaderDictionary dict = new HeaderDictionary
            {
                { ETag, new StringValues("\"1d20ac81ccb7b87\"") }
            };
            string expectedOutput = "\"1d20ac81ccb7b87:7576\"";

            // Act
            BrowserLinkMiddleWareUtil.AddToETag(dict, port);
            ResponseHeaders responseHeader = new ResponseHeaders(dict);
            string actualOutput = responseHeader.ETag.ToString();

            // Assert
            Assert.Equal(actualOutput, expectedOutput);
        }
    }
}
