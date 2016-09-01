using Xunit;

namespace Microsoft.VisualStudio.Web.BrowserLink
{
    public class PathUtilTest
    {
        [Fact]
        public void NormalizeDirectoryPath_ChangesSlashesToBackslashes()
        {
            // Arrange
            string input = @"c:/my/project/path/";
            string expected = @"c:\my\project\path\";

            // Act
            string result = PathUtil.NormalizeDirectoryPath(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void NormalizeDirectoryPath_AddsTrailingSlash()
        {
            // Arrange
            string input = @"c:/my/project/path";
            string expected = @"c:\my\project\path\";

            // Act
            string result = PathUtil.NormalizeDirectoryPath(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void NormalizeDirectoryPath_ChangesToLowercase()
        {
            // Arrange
            string input = @"C:/My/Project/Path/";
            string expected = @"c:\my\project\path\";

            // Act
            string result = PathUtil.NormalizeDirectoryPath(input);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
