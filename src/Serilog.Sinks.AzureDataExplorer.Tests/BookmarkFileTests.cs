// Copyright 2014 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
