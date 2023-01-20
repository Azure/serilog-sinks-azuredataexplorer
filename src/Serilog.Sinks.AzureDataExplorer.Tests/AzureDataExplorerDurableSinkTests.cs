using Serilog.Sinks.AzureDataExplorer.Sinks;

namespace Serilog.Sinks.AzureDataExplorer.Tests
{
    public class AzureDataExplorerDurableSinkTests
    {
        [Fact]
        public void AzureDataExplorerDurableSink_Throws_ArgumentNullException_For_Null_Options()
        {
            Assert.Throws<ArgumentNullException>(() => new AzureDataExplorerDurableSink(null));
        }

        [Fact]
        public void AzureDataExplorerDurableSink_Throws_ArgumentNullException_For_Null_DatabaseName()
        {
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = null,
                TableName = "test",
                IngestionEndpointUri = "https://test.com",
                BufferBaseFileName = "test",
                FlushImmediately = true
            };

            Assert.Throws<ArgumentNullException>(() => new AzureDataExplorerDurableSink(options));
        }

        [Fact]
        public void AzureDataExplorerDurableSink_Throws_ArgumentNullException_For_Null_TableName()
        {
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = "test",
                TableName = null,
                IngestionEndpointUri = "https://test.com",
                BufferBaseFileName = "test",
                FlushImmediately = true
            };
            Assert.Throws<ArgumentNullException>(() => new AzureDataExplorerDurableSink(options));
        }

        [Fact]
        public void AzureDataExplorerDurableSink_Throws_ArgumentNullException_For_Null_IngestionEndpointUri()
        {
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = "test",
                TableName = "test",
                IngestionEndpointUri = null,
                BufferBaseFileName = "test",
                FlushImmediately = true
            };

            Assert.Throws<ArgumentNullException>(() => new AzureDataExplorerDurableSink(options));
        }

        [Fact]
        public void AzureDataExplorerDurableSink_Throws_ArgumentException_For_Empty_BufferBaseFileName()
        {
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = "test",
                TableName = "test",
                IngestionEndpointUri = "https://test.com",
                BufferBaseFileName = "",
                FlushImmediately = true
            };

            Assert.Throws<ArgumentException>(() => new AzureDataExplorerDurableSink(options));
        }
    }
}

