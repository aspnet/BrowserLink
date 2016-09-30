using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.VisualStudio.Web.BrowserLink.Test
{
    public class BrowserLinkMiddleWareUtilTest
    {
        private const string IfNoneMatch = "If-None-Match";
        private const string IfModifiedSince = "If-Modified-Since";
        private const string ETag = "ETag";

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
        public void GetCurrentPort_NullConnectionString()
        {
            // Act
            int currentPort = BrowserLinkMiddleWareUtil.GetCurrentPort(null);

            // Assert
            Assert.Equal(currentPort, -1);
        }

        [Fact]
        public void GetCurrentPort_ContainsNoPortNumber()
        {
            // Arrange
            string connectionString = "http://localhost/skawia";

            // Act
            int currentPort = BrowserLinkMiddleWareUtil.GetCurrentPort(connectionString);

            // Assert
            Assert.Equal(currentPort, 80);
        }

        [Fact]
        public void GetCurrentPort_ContainsInvalidPortNumber()
        {
            // Arrange
            string connectionString = "http://localhost:3sk4/acskakdjaksa";

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
                { IfNoneMatch, new StringValues("\"1d20ac81ccb7b87:7576\"") }
            };
            RequestHeaders requestHeader = new RequestHeaders(dict);

            // Act
            List<int> requestPortList = BrowserLinkMiddleWareUtil.GetRequestPort(requestHeader);

            // Assert
            Assert.True(BrowserLinkMiddleWareUtil.IfMatch(requestPortList, 7576));
        }

        [Fact]
        public void GetRequestPort_EmptyIfNoneMatch()
        {
            // Arrange
            IHeaderDictionary dict = new HeaderDictionary
            {
                { IfNoneMatch, new StringValues("\"\"") }
            };
            RequestHeaders requestHeader = new RequestHeaders(dict);

            // Act
            List<int> requestPortList = BrowserLinkMiddleWareUtil.GetRequestPort(requestHeader);

            // Assert
            Assert.Equal(requestPortList.Count, 0);
        }

        [Fact]
        public void GetRequestPort_NullIfNoneMatch()
        {
            string[] strings = { };
            IHeaderDictionary dict = new HeaderDictionary
            {
                { IfNoneMatch, new StringValues(strings) }
            };
            RequestHeaders requestHeader = new RequestHeaders(dict);
            requestHeader.IfNoneMatch = null;

            // Act
            List<int> requestPortList = BrowserLinkMiddleWareUtil.GetRequestPort(requestHeader);

            // Assert
            Assert.Equal(requestPortList.Count, 0);
        }

        [Fact]
        public void GetRequestPort_NoIfNoneMatchEntry()
        {
            // Arrange
            IHeaderDictionary dict = new HeaderDictionary();
            RequestHeaders requestHeader = new RequestHeaders(dict);

            // Act
            List<int> requestPortList = BrowserLinkMiddleWareUtil.GetRequestPort(requestHeader);

            // Assert
            Assert.Equal(requestPortList.Count, 0);
        }

        [Fact]
        public void GetRequestPort_InvalidPortNumber()
        {
            // Arrange
            IHeaderDictionary dict = new HeaderDictionary
            {
                { IfNoneMatch, new StringValues("\"1d20ac81ccb7b87:afrq\"") }
            };
            RequestHeaders requestHeader = new RequestHeaders(dict);

            // Act
            List<int> requestPortList = BrowserLinkMiddleWareUtil.GetRequestPort(requestHeader);

            // Assert
            Assert.Equal(requestPortList.Count, 0);
        }

        [Fact]
        public void GetRequestPort_NoPortNumber()
        {
            // Arrange
            IHeaderDictionary dict = new HeaderDictionary
            {
                { IfNoneMatch, new StringValues("\"1d20ac81ccb7b87\"") }
            };
            RequestHeaders requestHeader = new RequestHeaders(dict);

            // Act
            List<int> requestPortList = BrowserLinkMiddleWareUtil.GetRequestPort(requestHeader);

            // Assert
            Assert.Equal(requestPortList.Count, 0);
        }

        [Fact]
        public void GetRequestPort_MultipleETagOneValue()
        {
            // Arrange
            string[] strings = { "\"1d20ac81ccb7b87:7576\"", "\"1sjaeuald13js17\"", "\"siela139s39aks1:7576\"" };
            IHeaderDictionary dict = new HeaderDictionary
            {
                { IfNoneMatch, new StringValues(strings) }
            };
            RequestHeaders requestHeader = new RequestHeaders(dict);

//            IList<EntityTagHeaderValue> list = requestHeader.IfNoneMatch;
//            list.Add(new EntityTagHeaderValue("\"1d20ac81ccb7b87:7576\""));
//            list.Add(new EntityTagHeaderValue("\"1sjaeuald13js17\""));
//            list.Add(new EntityTagHeaderValue("\"siela139s39aks1:7576\""));
//            requestHeader.IfNoneMatch = list;

            // Act
            List<int> requestPortList = BrowserLinkMiddleWareUtil.GetRequestPort(requestHeader);

            // Assert
            Assert.True(BrowserLinkMiddleWareUtil.IfMatch(requestPortList, 7576));
        }

        [Fact]
        public void GetRequestPort_MultipleETagDifferentValues()
        {
            // Arrange
            string[] strings = { "\"1d20ac81ccb7b87:7576\"", "\"1sjaeuald13js17:1356\"", "\"siela139s39aks1:8765\"" };
            IHeaderDictionary dict = new HeaderDictionary
            {
                { IfNoneMatch, new StringValues(strings) }
            };
            RequestHeaders requestHeader = new RequestHeaders(dict);

            // Act
            List<int> requestPortList = BrowserLinkMiddleWareUtil.GetRequestPort(requestHeader);

            // Assert
            Assert.True(BrowserLinkMiddleWareUtil.IfMatch(requestPortList, 7576));
            Assert.True(BrowserLinkMiddleWareUtil.IfMatch(requestPortList, 1356));
            Assert.True(BrowserLinkMiddleWareUtil.IfMatch(requestPortList, 8765));
        }

        [Fact]
        public void DeletePortFromEtag_ContainsPortNumber()
        {
            // Arrange
            IHeaderDictionary dict = new HeaderDictionary
            {
                { IfNoneMatch, new StringValues("\"1d20ac81ccb7b87:7576\"") }
            };
            RequestHeaders requestHeader = new RequestHeaders(dict);
            string expectedOutput = "\"1d20ac81ccb7b87\"";

            // Act
            BrowserLinkMiddleWareUtil.DeletePortFromETag(requestHeader);
            string actualOutput = requestHeader.IfNoneMatch[0].ToString();

            // Assert
            Assert.Equal(actualOutput, expectedOutput);         
        }

        [Fact]
        public void DeletePortFromEtag_ContainsMultipleETags()
        {
            // Arrange
            string[] strings = { "\"1d20ac81ccb7b87:7576\"", "\"1sjaeuald13js17:1356\"", "\"siela139s39aks1:8765\"" };
            IHeaderDictionary dict = new HeaderDictionary
            {
                { IfNoneMatch, new StringValues(strings) }
            };
            RequestHeaders requestHeader = new RequestHeaders(dict);

            // Act
            BrowserLinkMiddleWareUtil.DeletePortFromETag(requestHeader);

            // Assert
            Assert.Equal(requestHeader.IfNoneMatch[0].ToString(), "\"1d20ac81ccb7b87\"");
            Assert.Equal(requestHeader.IfNoneMatch[1].ToString(), "\"1sjaeuald13js17\"");
            Assert.Equal(requestHeader.IfNoneMatch[2].ToString(), "\"siela139s39aks1\"");
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
            ResponseHeaders responseHeader = new ResponseHeaders(dict);
            BrowserLinkMiddleWareUtil.AddToETag(responseHeader, port);
            string actualOutput = responseHeader.ETag.ToString();

            // Assert
            Assert.Equal(actualOutput, expectedOutput);
        }

        [Fact]
        public void AddToETag_NoTrailingQuote()
        {
            // Arrange
            int port = 7576;
            IHeaderDictionary dict = new HeaderDictionary
            {
                { ETag, new StringValues("1d20ac81ccb7b87") }
            };

            // Act
            ResponseHeaders responseHeader = new ResponseHeaders(dict);
            BrowserLinkMiddleWareUtil.AddToETag(responseHeader, port);


            // Assert
            Assert.Null(responseHeader.ETag);
        }

        [Fact]
        public void AddToETag_EmptyETag()
        {
            // Arrange
            int port = 7576;
            IHeaderDictionary dict = new HeaderDictionary
            {
                { ETag, new StringValues("\"\"") }
            };
            string expectedOutput = "\":7576\"";

            // Act
            ResponseHeaders responseHeader = new ResponseHeaders(dict);
            BrowserLinkMiddleWareUtil.AddToETag(responseHeader, port);
            string actualOutput = responseHeader.ETag.ToString();

            // Assert
            Assert.Equal(actualOutput, expectedOutput);
        }
        
        [Fact]
        public void FilterRequestHeader_PortsMatch()
        {
            // Arrange
            IHeaderDictionary dict = new HeaderDictionary
            {
                { IfNoneMatch, new StringValues("\"1d20ac81ccb7b87:7576\"") }
            };
            RequestHeaders requestHeader = new RequestHeaders(dict);
            string connectionString = "http://localhost:7576/acskakdjaksa";

            // Act
            BrowserLinkMiddleWareUtil.FilterRequestHeader(requestHeader, connectionString);

            // Assert
            Assert.Equal(requestHeader.IfNoneMatch[0].ToString(), "\"1d20ac81ccb7b87\"");
        }

        public void FilterRequestHeader_PortsNotMatch()
        {
            // Arrange
            IHeaderDictionary dict = new HeaderDictionary
            {
                { IfNoneMatch, new StringValues("\"1d20ac81ccb7b87:7576\"") },
                { IfModifiedSince, new StringValues("Fri, 24 Jun 2016 18:03:04 GMT") }
            };
            RequestHeaders requestHeader = new RequestHeaders(dict);
            string connectionString = "http://localhost:7577/acskakdjaksa";

            // Act
            BrowserLinkMiddleWareUtil.FilterRequestHeader(requestHeader, connectionString);

            // Assert
            Assert.Null(requestHeader.IfNoneMatch);
            Assert.Null(requestHeader.IfModifiedSince);
        }

        public void FilterRequestHeader_NoPortInConnectionString()
        {
            // Arrange
            IHeaderDictionary dict = new HeaderDictionary
            {
                { IfNoneMatch, new StringValues("\"1d20ac81ccb7b87:7576\"") },
                { IfModifiedSince, new StringValues("Fri, 24 Jun 2016 18:03:04 GMT") }
            };
            RequestHeaders requestHeader = new RequestHeaders(dict);
            string connectionString = "http://localhost/acskakdjaksa";

            // Act
            BrowserLinkMiddleWareUtil.FilterRequestHeader(requestHeader, connectionString);

            // Assert
            Assert.Null(requestHeader.IfNoneMatch);
            Assert.Null(requestHeader.IfModifiedSince);
        }

        public void FilterRequestHeader_NoPortInETag()
        {
            // Arrange
            IHeaderDictionary dict = new HeaderDictionary
            {
                { IfNoneMatch, new StringValues("\"1d20ac81ccb7b87\"") },
                { IfModifiedSince, new StringValues("Fri, 24 Jun 2016 18:03:04 GMT") }
            };
            RequestHeaders requestHeader = new RequestHeaders(dict);
            string connectionString = "http://localhost:7576/acskakdjaksa";

            // Act
            BrowserLinkMiddleWareUtil.FilterRequestHeader(requestHeader, connectionString);

            // Assert
            Assert.Null(requestHeader.IfNoneMatch);
            Assert.Null(requestHeader.IfModifiedSince);
        }

        public void FilterRequestHeader_NoPortInEtagAndConnectionString()
        {
            // Arrange
            IHeaderDictionary dict = new HeaderDictionary
            {
                { IfNoneMatch, new StringValues("\"1d20ac81ccb7b87\"") },
                { IfModifiedSince, new StringValues("Fri, 24 Jun 2016 18:03:04 GMT") }
            };
            RequestHeaders requestHeader = new RequestHeaders(dict);
            string connectionString = "http://localhost:invalid/acskakdjaksa";

            // Act
            BrowserLinkMiddleWareUtil.FilterRequestHeader(requestHeader, connectionString);

            // Assert
            Assert.Null(requestHeader.IfNoneMatch);
            Assert.Null(requestHeader.IfModifiedSince);
        }
    }
}
