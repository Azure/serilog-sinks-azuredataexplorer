using Kusto.Data;
using Kusto.Data.Net.Client;
using Kusto.Data.Common;
using Serilog.Sinks.AzureDataExplorer.Extensions;

namespace Serilog.Sinks.AzureDataExplorer.Tests;

/*
 * This is an End to End Testcase which requires the following input to be set as environment variables
 * ingestionURI : ingestion URL of ADX
 * databaseName : database name 
 * bufferFileName : If durable mode is required, we need to mention the buffer file name
 * appId : Application client Id
 * appKey : Application client key
 * tenant : Authority
 * These E2E testcases are disabled by default,
 * to enable it
 * 1. please change the access specifier of this class (AzureDataExplorerDurableSinkE2ETests) to public
 * 2. remove the System.Diagnostics.CodeAnalysis.SuppressMessage
 */

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
internal class AzureDataExplorerDurableSinkE2ETests : IDisposable
{
    private string m_bufferBaseFileName;
    private readonly string m_generatedTableName;

    public AzureDataExplorerDurableSinkE2ETests()
    {
        Assert.NotNull(Environment.GetEnvironmentVariable("ingestionURI"));
        Assert.NotNull(Environment.GetEnvironmentVariable("databaseName"));
        Assert.NotNull(Environment.GetEnvironmentVariable("appId"));
        Assert.NotNull(Environment.GetEnvironmentVariable("appKey"));
        Assert.NotNull(Environment.GetEnvironmentVariable("tenant"));

        m_bufferBaseFileName = "";
        var randomInt = new Random().Next();
        m_generatedTableName = "Serilog_" + randomInt;
        var kcsb = new KustoConnectionStringBuilder(
                AzureDataExplorerSinkOptionsExtensions.GetClusterUrl(
                    Environment.GetEnvironmentVariable("ingestionURI")),
                Environment.GetEnvironmentVariable("databaseName"))
            .WithAadApplicationKeyAuthentication(Environment.GetEnvironmentVariable("appId"),
                Environment.GetEnvironmentVariable("appKey"), Environment.GetEnvironmentVariable("tenant"));
        using (var kustoClient = KustoClientFactory.CreateCslAdminProvider(kcsb))
        {
            var command = CslCommandGenerator.GenerateTableCreateCommand(m_generatedTableName,
                new[]
                {
                    Tuple.Create("Timestamp", "System.DateTime"), Tuple.Create("Level", "System.String"),
                    Tuple.Create("Message", "System.string"), Tuple.Create("Exception", "System.string"),
                    Tuple.Create("Properties", "System.Object"), Tuple.Create("Position", "System.Object"),
                    Tuple.Create("Elapsed", "System.Int32")
                });
            var alterBatchingPolicy = CslCommandGenerator.GenerateTableAlterIngestionBatchingPolicyCommand(
                Environment.GetEnvironmentVariable("databaseName"), m_generatedTableName,
                new IngestionBatchingPolicy(TimeSpan.FromSeconds(5), 10, 1024));
            var enableStreamingIngestion =
                CslCommandGenerator.GenerateTableAlterStreamingIngestionPolicyCommand(m_generatedTableName, true);
            kustoClient.ExecuteControlCommand(Environment.GetEnvironmentVariable("databaseName"), command);
            kustoClient.ExecuteControlCommand(Environment.GetEnvironmentVariable("databaseName"),
                alterBatchingPolicy);
            kustoClient.ExecuteControlCommand(Environment.GetEnvironmentVariable("databaseName"),
                enableStreamingIngestion);
        }
    }

    [Fact]
    public async Task Test_AzureDataExplorer_Durable_SerilogSink_Queued()
    {
        var randomInt = new Random().Next();
        m_bufferBaseFileName = Directory.GetCurrentDirectory() + "/" + randomInt + "/logger-buffer";
        if (!Directory.Exists(Directory.GetCurrentDirectory() + "/" + randomInt))
        {
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/" + randomInt);
        }
        var identifier = "Test_AzureDataExplorer_Durable_SerilogSink_Queued_Ingestion";

        using (var log = new LoggerConfiguration()
                   .MinimumLevel.Verbose()
                   .WriteTo.AzureDataExplorerSink(new AzureDataExplorerSinkOptions
                   {
                       IngestionEndpointUri = Environment.GetEnvironmentVariable("ingestionURI"),
                       DatabaseName = Environment.GetEnvironmentVariable("databaseName"),
                       BatchPostingLimit = 10,
                       Period = TimeSpan.FromSeconds(5),
                       TableName = m_generatedTableName,
                       BufferBaseFileName = m_bufferBaseFileName,
                       BufferFileRollingInterval = RollingInterval.Day,
                       FlushImmediately = true,
                       ColumnsMapping = new[]
                       {
                           new SinkColumnMapping
                               { ColumnName = "Timestamp", ColumnType = "datetime", ValuePath = "$.Timestamp" },
                           new SinkColumnMapping { ColumnName = "Level", ColumnType = "string", ValuePath = "$.Level" },
                           new SinkColumnMapping
                               { ColumnName = "Message", ColumnType = "string", ValuePath = "$.Message" },
                           new SinkColumnMapping
                               { ColumnName = "Exception", ColumnType = "string", ValuePath = "$.Exception" },
                           new SinkColumnMapping
                               { ColumnName = "Properties", ColumnType = "dynamic", ValuePath = "$.Properties" },
                           new SinkColumnMapping
                               { ColumnName = "Position", ColumnType = "dynamic", ValuePath = "$.Properties.Position" },
                           new SinkColumnMapping
                               { ColumnName = "Elapsed", ColumnType = "int", ValuePath = "$.Properties.Elapsed" },
                       }
                   }.WithAadApplicationKey(Environment.GetEnvironmentVariable("appId"),
                       Environment.GetEnvironmentVariable("appKey"), Environment.GetEnvironmentVariable("tenant")))
                   .CreateLogger())
        {
            var position = new { Latitude = 25, Longitude = 134 };
            var elapsedMs = 34;

            log.Verbose(identifier + " Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Information(identifier + "Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Warning(identifier + "Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Error(identifier + "Exception occurred", "Zohar Processed {@Position} in {Elapsed:000} ms.", position,
                elapsedMs);
            log.Debug(identifier + "Processed {@Position} in {Elapsed:000} ms. ", position, elapsedMs);
            log.Verbose(identifier + "Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Information(identifier + "Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Warning(identifier + "Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Error(identifier + "Exception occurred", "Zohar Processed {@Position} in {Elapsed:000} ms.", position,
                elapsedMs);
            log.Debug(identifier + "Processed {@Position} in {Elapsed:000} ms. ", position, elapsedMs);
            log.Verbose(identifier + "Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Information(identifier + "Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);

        }

        await Task.Delay(10000);

        int lineCount = 0;
        foreach (string file in Directory.EnumerateFiles(Directory.GetCurrentDirectory() + "/" + randomInt, "*.clef"))
        {
            lineCount += System.IO.File.ReadLines(file).Count();
        }
        Assert.Equal(12, lineCount);

        int noOfRecordsIngested = GetNoOfRecordsIngestedInAdx(identifier);
        Assert.Equal(12, noOfRecordsIngested);
    }

    [Fact]
    public async void test_AzureDataExplorer_Serilog_Sink_With_Buffer_LogLevelSwitch()
    {
        const string identifier = "test_AzureDataExplorer_Serilog_Sink_With_Buffer_LogLevelSwitch";
        var randomInt = new Random().Next();
        m_bufferBaseFileName = Directory.GetCurrentDirectory() + "/" + randomInt + "/logger-buffer";
        if (!Directory.Exists(Directory.GetCurrentDirectory() + "/" + randomInt))
        {
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/" + randomInt);
        }

        using (var log = new LoggerConfiguration()
                   .MinimumLevel.Verbose()
                   .WriteTo.AzureDataExplorerSink(new AzureDataExplorerSinkOptions
                   {
                       IngestionEndpointUri = Environment.GetEnvironmentVariable("ingestionURI"),
                       BatchPostingLimit = 10,
                       Period = TimeSpan.FromMilliseconds(1000),
                       DatabaseName = Environment.GetEnvironmentVariable("databaseName"),
                       TableName = m_generatedTableName,
                       BufferBaseFileName = m_bufferBaseFileName,
                       BufferFileRollingInterval = RollingInterval.Day,
                       BufferFileLoggingLevelSwitch = new Core.LoggingLevelSwitch(Events.LogEventLevel.Error),
                       FlushImmediately = true,
                       ColumnsMapping = new[]
                       {
                           new SinkColumnMapping
                               { ColumnName = "Timestamp", ColumnType = "datetime", ValuePath = "$.Timestamp" },
                           new SinkColumnMapping { ColumnName = "Level", ColumnType = "string", ValuePath = "$.Level" },
                           new SinkColumnMapping
                               { ColumnName = "Message", ColumnType = "string", ValuePath = "$.Message" },
                           new SinkColumnMapping
                               { ColumnName = "Exception", ColumnType = "string", ValuePath = "$.Exception" },
                           new SinkColumnMapping
                               { ColumnName = "Properties", ColumnType = "dynamic", ValuePath = "$.Properties" },
                           new SinkColumnMapping
                               { ColumnName = "Position", ColumnType = "dynamic", ValuePath = "$.Properties.Position" },
                           new SinkColumnMapping
                               { ColumnName = "Elapsed", ColumnType = "int", ValuePath = "$.Properties.Elapsed" },
                       }
                   }.WithAadApplicationKey(Environment.GetEnvironmentVariable("appId"),
                       Environment.GetEnvironmentVariable("appKey"), Environment.GetEnvironmentVariable("tenant")))
                   .CreateLogger())
        {
            var position = new { Latitude = 25, Longitude = 134 };
            var elapsedMs = 34;

            log.Verbose(identifier + " Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Information(identifier + "Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Warning(identifier + "Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Error(identifier + "Exception occurred", "Zohar Processed {@Position} in {Elapsed:000} ms.", position,
                elapsedMs);
            log.Debug(identifier + "Processed {@Position} in {Elapsed:000} ms. ", position, elapsedMs);
            log.Verbose(identifier + "Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Information(identifier + "Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Warning(identifier + "Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Error(identifier + "Exception occurred", "Zohar Processed {@Position} in {Elapsed:000} ms.", position,
                elapsedMs);
            log.Debug(identifier + "Processed {@Position} in {Elapsed:000} ms. ", position, elapsedMs);
            log.Verbose(identifier + "Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Information(identifier + "Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
        }

        await Task.Delay(10000);

        int lineCount = 0;
        foreach (string file in Directory.EnumerateFiles(Directory.GetCurrentDirectory() + "/" + randomInt, "*.clef"))
        {
            lineCount += System.IO.File.ReadLines(file).Count();
        }
        Assert.Equal(2, lineCount);

        int noOfRecordsIngested =
            GetNoOfRecordsIngestedInAdx(identifier);
        Assert.Equal(2, noOfRecordsIngested);
    }

    private int GetNoOfRecordsIngestedInAdx(string searchString)
    {
        var kcsbEx = new KustoConnectionStringBuilder(
                AzureDataExplorerSinkOptionsExtensions.GetClusterUrl(
                    Environment.GetEnvironmentVariable("ingestionURI")),
                Environment.GetEnvironmentVariable("databaseName"))
            .WithAadApplicationKeyAuthentication(Environment.GetEnvironmentVariable("appId"),
                Environment.GetEnvironmentVariable("appKey"), Environment.GetEnvironmentVariable("tenant"));
        var noOfRecordsIngested = 0;
        using (var queryProvider = KustoClientFactory.CreateCslQueryProvider(kcsbEx))
        {
            var query = m_generatedTableName + " | where Message contains '" + searchString + "' | count";
            var clientRequestProperties = new ClientRequestProperties() { ClientRequestId = Guid.NewGuid().ToString() };
            using (var reader = queryProvider.ExecuteQuery(Environment.GetEnvironmentVariable("databaseName"), query,
                       clientRequestProperties))
            {
                // Read HowManyRecords
                while (reader.Read())
                {
                    noOfRecordsIngested = (int)reader.GetInt64(0);
                }
            }
        }

        return noOfRecordsIngested;
    }

    public void Dispose()
    {
        var kcsb = new KustoConnectionStringBuilder(
                AzureDataExplorerSinkOptionsExtensions.GetClusterUrl(
                    Environment.GetEnvironmentVariable("ingestionURI")),
                Environment.GetEnvironmentVariable("databaseName"))
            .WithAadApplicationKeyAuthentication(Environment.GetEnvironmentVariable("appId"),
                Environment.GetEnvironmentVariable("appKey"), Environment.GetEnvironmentVariable("tenant"));
        using (var queryProvider = KustoClientFactory.CreateCslAdminProvider(kcsb))
        {
            var command = CslCommandGenerator.GenerateTableDropCommand(m_generatedTableName);
            var clientRequestProperties = new ClientRequestProperties()
            { ClientRequestId = Guid.NewGuid().ToString() };
            queryProvider.ExecuteControlCommand(Environment.GetEnvironmentVariable("databaseName"), command,
                clientRequestProperties);
        }
    }
}
