using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Web.BrowserLink.Test
{
    public class HostConnectionUtilTest
    {
        private List<string> SampleConnectionDataV1 = new List<string>
        {
            @"http://localhost:1234/ABCDEF123456-1/browserLink",
            @"https://localhost:44332/ABCDEF123456-1/browserLink",
            @"C:\path\to\My Projects\WebApplication1\src\WebApplication1"
        };

        private List<string> SampleConnectionDataV2 = new List<string>
        {
            @"host-name:localhost",
            @"http-port:1234",
            @"https-port:44332",
            @"verb-fetch-script:browserLink",
            @"verb-inject-script:injectScriptLink",
            @"verb-mapping-data:sendMappingData",
            @"verb-server-data:sendServerData",
            @"project:C:\path\to\My Projects\WebApplication1\src\WebApplication1;ABCDEF123456-1",
        };

        private const string FileNameBase = "BrowserLink.12345";
        private const string FileName = FileNameBase + BrowserLinkConstants.Version2Suffix;

        private const string ExpectedRequestSignalName = FileNameBase + BrowserLinkConstants.RequestSignalSuffix;
        private const string ExpectedReadySignalName = FileNameBase + BrowserLinkConstants.ReadySignalSuffix;

        private const string ExpectedProjectPath = @"c:\path\to\my projects\webapplication1\src\webapplication1\";

        private const string ExpectedHttpConnectionString = "http://localhost:1234/ABCDEF123456-1/browserLink";
        private const string ExpectedHttpsConnectionString = "https://localhost:44332/ABCDEF123456-1/browserLink";

        private const string ExpectedInjectScriptVerb = "http://localhost:1234/ABCDEF123456-1/injectScriptLink";
        private const string ExpectedMappingDataVerb = "http://localhost:1234/ABCDEF123456-1/sendMappingData";
        private const string ExpectedServerDataVerb = "http://localhost:1234/ABCDEF123456-1/sendServerData";

        [Fact]
        public void ArteryConnectionUtil_ParseV1ConnectionData_ParsesBasicConnection()
        {
            Test_ParseV1ConnectionData();
        }

        [Fact]
        public void ArteryConnectionUtil_ParseV1ConnectionData_WithMissingHttpConnectionString()
        {
            SampleConnectionDataV1[0] = String.Empty;

            Test_ParseV1ConnectionData(
                expectedHttpConnectionString: String.Empty,
                expectedInjectScriptVerb: null,
                expectedMappingDataVerb: null);
        }

        [Fact]
        public void ArteryConnectionUtil_ParseV1ConnectionData_WithMultipleProjects()
        {
            SampleConnectionDataV1.Add(@"C:\path\to\My Projects\OtherWeb1\OtherWeb1");

            Test_ParseV1ConnectionData(
                expectedProjectPaths: new string[]
                {
                    ExpectedProjectPath,
                    @"c:\path\to\my projects\otherweb1\otherweb1\"
                });
        }

        [Fact]
        public void ArteryConnectionUtil_ParseV1ConnectionData_WithNoProjects()
        {
            SampleConnectionDataV1.RemoveAt(2);

            Test_ParseV1ConnectionData_ShouldFail();
        }

        [Fact]
        public void ArteryConnectionUtil_WithTooFewLines()
        {
            // Arrange
            SampleConnectionDataV1.RemoveAt(2);
            SampleConnectionDataV1.RemoveAt(1);

            Test_ParseV1ConnectionData_ShouldFail();
        }

        private void Test_ParseV1ConnectionData(
            string fileName = FileNameBase,
            string expectedHttpConnectionString = ExpectedHttpConnectionString,
            string expectedHttpsConnectionString = ExpectedHttpsConnectionString,
            string expectedReadingSignalName = ExpectedReadySignalName,
            string expectedRequestSignalName = ExpectedRequestSignalName,
            string expectedInjectScriptVerb = ExpectedInjectScriptVerb,
            string expectedMappingDataVerb = ExpectedMappingDataVerb,
            string expectedServerDataVerb = null,
            IEnumerable<string> expectedProjectPaths = null)
        {
            // Arrange & act
            var result = HostConnectionUtil.ParseV1ConnectionData(fileName, SampleConnectionDataV1, out HostConnectionData connection);

            // Assert
            Assert.True(result, "ParseV1ConnectionData failed.");
            Assert.NotNull(connection);

            Assert.Equal(expectedHttpConnectionString, connection.ConnectionString);
            Assert.Equal(expectedHttpsConnectionString, connection.SslConnectionString);
            Assert.Equal(expectedRequestSignalName, connection.RequestSignalName);
            Assert.Equal(ExpectedReadySignalName, connection.ReadySignalName);
            Assert.Equal(expectedInjectScriptVerb, connection.InjectScriptVerb);
            Assert.Equal(expectedMappingDataVerb, connection.MappingDataVerb);
            Assert.Equal(expectedServerDataVerb, connection.ServerDataVerb);
            
            Assert.NotNull(connection.ProjectPaths);

            if (expectedProjectPaths == null)
            {
                expectedProjectPaths = new string[] { ExpectedProjectPath };
            }

            IEnumerator<string> expectedIter = expectedProjectPaths.GetEnumerator();
            IEnumerator<string> actualIter = connection.ProjectPaths.GetEnumerator();

            while (expectedIter.MoveNext())
            {
                Assert.True(actualIter.MoveNext(), "Expected to find <{0}> before the end of the {1} list");
                Assert.Equal(expectedIter.Current, actualIter.Current);
            }

            Assert.False(actualIter.MoveNext(), "Too many items in the {0} list");
        }

        private void Test_ParseV1ConnectionData_ShouldFail()
        {
            // Act
            var result = HostConnectionUtil.ParseV1ConnectionData(FileName, SampleConnectionDataV1, out HostConnectionData connection);

            // Assert
            Assert.False(result, "ParseV1ConnectionData should fail.");
            Assert.Null(connection);
        }

        [Fact]
        public void ArteryConnectionUtil_ParseV2ConnectionData_ParsesBasicConnection()
        {
            Test_ParseV2ConnectionData();
        }

        [Fact]
        public void ArteryConnectionUtil_ParseV2ConnectionData_MissingHostName()
        {
            SampleConnectionDataV2.RemoveAt(0);

            Test_ParseV2ConnectionData(
                expectedHttpConnectionString: String.Empty,
                expectedHttpsConnectionString: String.Empty,
                expectedInjectScriptVerb: null,
                expectedMappingDataVerb: null,
                expectedServerDataVerb: null);
        }

        [Fact]
        public void ArteryConnectionUtil_ParseV2ConnectionData_MissingHttpPort()
        {
            SampleConnectionDataV2.RemoveAt(1);

            Test_ParseV2ConnectionData(
                expectedHttpConnectionString: String.Empty,
                expectedInjectScriptVerb: null,
                expectedMappingDataVerb: null,
                expectedServerDataVerb: null);
        }

        [Fact]
        public void ArteryConnectionUtil_ParseV2ConnectionData_MissingHttpsPort()
        {
            SampleConnectionDataV2.RemoveAt(2);

            Test_ParseV2ConnectionData(
                expectedHttpsConnectionString: String.Empty);
        }

        [Fact]
        public void ArteryConnectionUtil_ParseV2ConnectionData_MissingFetchScriptVerb()
        {
            SampleConnectionDataV2.RemoveAt(3);

            Test_ParseV2ConnectionData(
                expectedHttpConnectionString: String.Empty,
                expectedHttpsConnectionString: String.Empty,
                expectedInjectScriptVerb: null,
                expectedMappingDataVerb: null,
                expectedServerDataVerb: null);
        }

        [Fact]
        public void ArteryConnectionUtil_ParseV2ConnectionData_MissingInjectScriptVerb()
        {
            SampleConnectionDataV2.RemoveAt(4);

            Test_ParseV2ConnectionData(
                expectedInjectScriptVerb: null);
        }

        [Fact]
        public void ArteryConnectionUtil_ParseV2ConnectionData_MissingMappingDataVerb()
        {
            SampleConnectionDataV2.RemoveAt(5);

            Test_ParseV2ConnectionData(
                expectedMappingDataVerb: null);
        }

        [Fact]
        public void ArteryConnectionUtil_ParseV2ConnectionData_MissingServerDataVerb()
        {
            SampleConnectionDataV2.RemoveAt(6);

            Test_ParseV2ConnectionData(
                expectedServerDataVerb: null);
        }

        [Fact]
        public void ArteryConnectionUtil_ParseV2ConnectionData_MissingProject()
        {
            SampleConnectionDataV2.RemoveAt(7);

            Test_ParseV2ConnectionData(
                expectedNumberOfConnections: 0);
        }

        [Fact]
        public void ArteryConnectionUtil_ParseV2ConnectionData_MultipleProjects()
        {
            SampleConnectionDataV2.Add(@"project:E:\WebToolsExtensions\SampleWebProject;ABC123");
            SampleConnectionDataV2.Add(@"project:D:\school\SampleWebProject2\SampleWebProject2;xyz789");

            Test_ParseV2ConnectionData(
                expectedNumberOfConnections: 3);

            Test_ParseV2ConnectionData(
                expectedNumberOfConnections: 3,
                expectedProjectPath: @"e:\webtoolsextensions\samplewebproject\",
                expectedHttpConnectionString: "http://localhost:1234/ABC123/browserLink",
                expectedHttpsConnectionString: "https://localhost:44332/ABC123/browserLink",
                expectedInjectScriptVerb: "http://localhost:1234/ABC123/injectScriptLink",
                expectedMappingDataVerb: "http://localhost:1234/ABC123/sendMappingData",
                expectedServerDataVerb: "http://localhost:1234/ABC123/sendServerData");

            Test_ParseV2ConnectionData(
                expectedNumberOfConnections: 3,
                expectedProjectPath: @"d:\school\samplewebproject2\samplewebproject2\",
                expectedHttpConnectionString: "http://localhost:1234/xyz789/browserLink",
                expectedHttpsConnectionString: "https://localhost:44332/xyz789/browserLink",
                expectedInjectScriptVerb: "http://localhost:1234/xyz789/injectScriptLink",
                expectedMappingDataVerb: "http://localhost:1234/xyz789/sendMappingData",
                expectedServerDataVerb: "http://localhost:1234/xyz789/sendServerData");
        }

        [Fact]
        public void ArteryConnectionUtil_ParseV2ConnectionData_InvalidConnectionString()
        {
            SampleConnectionDataV2[0] = "host-name:local+host";

            Test_ParseV2ConnectionData(
                expectedHttpConnectionString: "http://local+host:1234/ABCDEF123456-1/browserLink",
                expectedHttpsConnectionString: "https://local+host:44332/ABCDEF123456-1/browserLink",
                expectedInjectScriptVerb: null,
                expectedMappingDataVerb: null,
                expectedServerDataVerb: null);
        }

        [Fact]
        public void ArteryConnectionUtil_ParseV2ConnectionData_AcceptsProjectsOnly()
        {
            string projectLine = SampleConnectionDataV2[7];
            SampleConnectionDataV2.Clear();
            SampleConnectionDataV2.Add(projectLine);

            Test_ParseV2ConnectionData(
                expectedHttpConnectionString: String.Empty,
                expectedHttpsConnectionString: String.Empty,
                expectedInjectScriptVerb: null,
                expectedMappingDataVerb: null,
                expectedServerDataVerb: null);
        }

        [Fact]
        public void ArteryConnectionutil_ParseV2ConnectionData_AcceptsLinesInReverseOrder()
        {
            SampleConnectionDataV2.Reverse();

            Test_ParseV2ConnectionData();
        }

        private void Test_ParseV2ConnectionData(
            string fileName = FileName,
            int expectedNumberOfConnections = 1,
            string expectedRequestSignalName = ExpectedRequestSignalName,
            string expectedReadySignalName = ExpectedReadySignalName,
            string expectedProjectPath = ExpectedProjectPath,
            string expectedHttpConnectionString = ExpectedHttpConnectionString,
            string expectedHttpsConnectionString = ExpectedHttpsConnectionString,
            string expectedInjectScriptVerb = ExpectedInjectScriptVerb,
            string expectedMappingDataVerb = ExpectedMappingDataVerb,
            string expectedServerDataVerb = ExpectedServerDataVerb)
        {
            // Act
            var result = HostConnectionUtil.ParseV2ConnectionData(fileName, SampleConnectionDataV2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedNumberOfConnections, result.Count());

            if (expectedNumberOfConnections > 0)
            {
                Assert.False(result.Any(x => x == null), "List should not contain null connections");

                HostConnectionData connection = result.Where(x => x.ProjectPaths.First() == expectedProjectPath).SingleOrDefault();
                Assert.NotNull(connection);

                Assert.Equal(expectedRequestSignalName, connection.RequestSignalName);
                Assert.Equal(expectedReadySignalName, connection.ReadySignalName);

                Assert.NotNull(connection.ProjectPaths);
                Assert.Single(connection.ProjectPaths);
                Assert.Equal(expectedProjectPath, connection.ProjectPaths.First());

                Assert.Equal(expectedHttpConnectionString, connection.ConnectionString);
                Assert.Equal(expectedHttpsConnectionString, connection.SslConnectionString);
                Assert.Equal(expectedInjectScriptVerb, connection.InjectScriptVerb);
                Assert.Equal(expectedMappingDataVerb, connection.MappingDataVerb);
                Assert.Equal(expectedServerDataVerb, connection.ServerDataVerb);
            }
        }
    }
}
