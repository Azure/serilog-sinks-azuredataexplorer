using System.Text;
using System.Text.RegularExpressions;
using Serilog.Sinks.AzureDataExplorer.Durable;

namespace Serilog.Sinks.AzureDataExplorer
{
    public class BookmarkFileTests
    {
        [Fact]
        public void TryReadBookmark_ReturnsNone_WhenFileIsEmpty()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            using (var bookmarkFile = new BookmarkFile(tempFile))
            {
                // Act
                var result = bookmarkFile.TryReadBookmark();

                // Assert
                Assert.Equal(FileSetPosition.None, result);
            }
        }

        [Fact]
        public void TryReadBookmark_ReturnsFileSetPosition_WhenFileContainsValidData()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            System.IO.File.WriteAllText(tempFile, "123:::file.txt", Encoding.UTF8);
            using (var bookmarkFile = new BookmarkFile(tempFile))
            {
                // Act
                var result = bookmarkFile.TryReadBookmark();

                // Assert
                Assert.Equal(123, result.NextLineStart);
                Assert.Equal("file.txt", result.File);
            }
        }

        [Fact]
        public async Task WriteBookmark_WritesFileSetPositionToFileAsync()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            using (var bookmarkFile = new BookmarkFile(tempFile))
            {
                var fileSetPosition = new FileSetPosition(456, "file2.txt");

                // Act
                bookmarkFile.WriteBookmark(fileSetPosition);

                // Assert
                Stream stream = System.IO.File.Open(tempFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                StreamReader streamReader = new StreamReader(stream);
                string str = await streamReader.ReadToEndAsync();
                Assert.Equal("456:::file2.txt", Regex.Replace(str, @"\t|\n|\r", ""));
            }
        }

        [Fact]
        public void Dispose_ClosesFileStream()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var bookmarkFile = new BookmarkFile(tempFile);

            // Act
            bookmarkFile.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => bookmarkFile.TryReadBookmark());
        }
    }
}
