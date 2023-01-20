using Serilog.Configuration;
using Serilog.Sinks.AzureDataExplorer.Extensions;

namespace Serilog.Sinks.AzureDataExplorer.Tests;

public class AzureDataExplorerSinkExtensionsTests
{
    [Fact]
    public void AzureDataExplorerSink_Creates_Valid_Sink_Configuration()
    {
        // Arrange
        var options = new AzureDataExplorerSinkOptions
        {
            DatabaseName = "mockDB",
            TableName = "mockTable",
            IngestionEndpointUri = "http://ingestionUri",
            BatchPostingLimit = 10,
            Period = TimeSpan.FromSeconds(10),
            QueueSizeLimit = 100
        };
        var loggerConfiguration = new LoggerConfiguration();

        // Act
        var sinkConfiguration = loggerConfiguration.WriteTo.AzureDataExplorerSink(options);

        // Assert
        Assert.NotNull(sinkConfiguration);
        Assert.IsType<LoggerConfiguration>(sinkConfiguration);
    }

    [Fact]
    public void AzureDataExplorerSink_Throws_Exception_For_Null_LoggerConfiguration()
    {
        // Arrange
        LoggerSinkConfiguration loggerConfiguration = null;
        var options = new AzureDataExplorerSinkOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            loggerConfiguration.AzureDataExplorerSink(options));
    }

    [Fact]
    public void AzureDataExplorerSink_Throws_Exception_For_Null_Options()
    {
        // Arrange
        var loggerConfiguration = new LoggerConfiguration();
        AzureDataExplorerSinkOptions options = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            loggerConfiguration.WriteTo.AzureDataExplorerSink(options));
    }
}
