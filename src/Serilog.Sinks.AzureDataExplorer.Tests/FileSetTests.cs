using System.Text;
using FluentAssertions;
using Serilog.Sinks.AzureDataExplorer.Durable;
using Serilog.Sinks.AzureDataExplorer.Extensions;

namespace Serilog.Sinks.AzureDataExplorer
{
    public class FileSetTests : IDisposable
    {
        private readonly string m_fileNameBase;
        private readonly string m_tempFileFullPathTemplate;
        private Dictionary<RollingInterval, string> m_bufferFileNames = null!;

        public FileSetTests()
        {
            m_fileNameBase = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            m_tempFileFullPathTemplate = m_fileNameBase + "-{0}.clef";
        }

        public void Dispose()
        {
            foreach (var bufferFileName in m_bufferFileNames.Values)
            {
                System.IO.File.Delete(bufferFileName);
            }
        }

        [Theory]
        [InlineData(RollingInterval.Day)]
        [InlineData(RollingInterval.Hour)]
        [InlineData(RollingInterval.Infinite)]
        [InlineData(RollingInterval.Minute)]
        [InlineData(RollingInterval.Month)]
        [InlineData(RollingInterval.Year)]
        // Ensures that from all presented files FileSet gets only files with specified rolling interval and not the others.  
        public void GetBufferFiles_ReturnsOnlySpecifiedTypeOfRollingFile(RollingInterval rollingInterval)
        {
            // Arrange
            var format = rollingInterval.GetFormat();
            m_bufferFileNames = GenerateFilesUsingFormat(format);
            var fileSet = new FileSet(m_fileNameBase, rollingInterval);
            var bufferFileForInterval = m_bufferFileNames[rollingInterval];

            // Act
            var bufferFiles = fileSet.GetBufferFiles();

            // Assert
            bufferFiles.Should().BeEquivalentTo(bufferFileForInterval);
        }

        /// <summary>
        /// Generates buffer files for all RollingIntervals and returns dictionary of {rollingInterval, fileName} pairs.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        private Dictionary<RollingInterval, string> GenerateFilesUsingFormat(string format)
        {
            var result = new Dictionary<RollingInterval, string>();
            foreach (var rollingInterval in Enum.GetValues(typeof(RollingInterval)))
            {
                var bufferFileName = string.Format(m_tempFileFullPathTemplate,
                    string.IsNullOrEmpty(format) ? string.Empty : new DateTime(2000, 1, 1).ToString(format));
                var lines = new[]
                {
                    rollingInterval.ToString()
                };
                // Important to use UTF8 with BOM if we are starting from 0 position 
                System.IO.File.WriteAllLines(bufferFileName, lines!, new UTF8Encoding(true));
                result.Add((RollingInterval)rollingInterval, bufferFileName);
            }

            return result;
        }
    }
}
