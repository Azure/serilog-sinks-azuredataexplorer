using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Serilog.Core;
using Serilog.Sinks.AzureDataExplorer.Extensions;

namespace Serilog.Sinks.AzureDataExplorer;

/*
 * This is an End to End Testcase which requires the following input to be set as environment variables
 * ingestionURI : ingestion URL of ADX
 * databaseName : database name 
 * bufferFileName : If durable mode is required, we need to mention the buffer file name, this option is not mandatory
 * appId : Application client Id
 * appKey : Application client key
 * tenant : Authority
 */

public class AzureDataExplorerSinkE2ETests : IDisposable
{
    private string? m_bufferBaseFileName;
    private readonly string? m_generatedTableName;
    private readonly KustoConnectionStringBuilder? m_kustoConnectionStringBuilder;
    private readonly IEnumerable<SinkColumnMapping> m_columnMappings;

    public AzureDataExplorerSinkE2ETests()
    {
        Assert.NotNull(Environment.GetEnvironmentVariable("ingestionURI"));
        Assert.NotNull(Environment.GetEnvironmentVariable("databaseName"));
        Assert.NotNull(Environment.GetEnvironmentVariable("appId"));
        Assert.NotNull(Environment.GetEnvironmentVariable("appKey"));
        Assert.NotNull(Environment.GetEnvironmentVariable("tenant"));
        m_bufferBaseFileName = "";
        var randomInt = new Random().Next();
        m_generatedTableName = "Serilog_" + randomInt;
        m_kustoConnectionStringBuilder = new KustoConnectionStringBuilder(
                AzureDataExplorerSinkOptionsExtensions.GetClusterUrl(
                    Environment.GetEnvironmentVariable("ingestionURI")),
                Environment.GetEnvironmentVariable("databaseName"))
            .WithAadApplicationKeyAuthentication(Environment.GetEnvironmentVariable("appId"),
                Environment.GetEnvironmentVariable("appKey"), Environment.GetEnvironmentVariable("tenant"));
        using (var kustoClient = KustoClientFactory.CreateCslAdminProvider(m_kustoConnectionStringBuilder))
        {
            var command = CslCommandGenerator.GenerateTableCreateCommand(m_generatedTableName,
                new[]
                {
                    Tuple.Create("Timestamp", "System.DateTime"),
                    Tuple.Create("Level", "System.String"),
                    Tuple.Create("Message", "System.string"),
                    Tuple.Create("Exception", "System.string"),
                    Tuple.Create("Properties", "System.Object"),
                    Tuple.Create("Position", "System.Object"),
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
        m_columnMappings = new[]
        {
            new SinkColumnMapping
            {
                ColumnName = "Timestamp", ColumnType = "datetime", ValuePath = "$.Timestamp"
            },
            new SinkColumnMapping
            {
                ColumnName = "Level", ColumnType = "string", ValuePath = "$.Level"
            },
            new SinkColumnMapping
            {
                ColumnName = "Message", ColumnType = "string", ValuePath = "$.Message"
            },
            new SinkColumnMapping
            {
                ColumnName = "Exception", ColumnType = "string", ValuePath = "$.Exception"
            },
            new SinkColumnMapping
            {
                ColumnName = "Properties", ColumnType = "dynamic", ValuePath = "$.Properties"
            },
            new SinkColumnMapping
            {
                ColumnName = "Position", ColumnType = "dynamic", ValuePath = "$.Properties.Position"
            },
            new SinkColumnMapping
            {
                ColumnName = "Elapsed", ColumnType = "int", ValuePath = "$.Properties.Elapsed"
            }
        };
    }

    [Theory]
    [InlineData("Test_AzureDataExplorer_Serilog_Sink_Queued_Ingestion_Durable", "durable", 10)]
    [InlineData("Test_AzureDataExplorer_Serilog_Sink_LogLevelSwitch_Durable", "durable", 2)]
    [InlineData("Test_AzureDataExplorer_Serilog_Sink_Queued_Ingestion_NonDurable", "non-durable", 10)]
    [InlineData("Test_AzureDataExplorer_Serilog_Sink_With_Streaming_NonDurable", "non-durable", 10)]
    public async Task Test_AzureDataExplorer_SerilogSink(string identifier, string runMode, int result)
    {
        var randomInt = new Random().Next();
        if (String.Equals(runMode, "durable"))
        {
            m_bufferBaseFileName = Directory.GetCurrentDirectory() + "/" + randomInt + "/logger-buffer";
            if (!Directory.Exists(Directory.GetCurrentDirectory() + "/" + randomInt))
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/" + randomInt);
            }
        }
        Logger log = GetSerilogAdxSink(identifier);

        var position = new
        {
            Latitude = 25, Longitude = 134
        };
        var elapsedMs = 34;

        log.Verbose(" {Identifier} Processed {@Position} in {Elapsed:000} ms.", identifier, position, elapsedMs);
        log.Information(" {Identifier} Processed {@Position} in {Elapsed:000} ms.", identifier, position, elapsedMs);
        log.Warning(" {Identifier} Processed {@Position} in {Elapsed:000} ms.", identifier, position, elapsedMs);
        log.Error(" {Identifier} Zohar Processed {@Position} in {Elapsed:000} ms.", identifier, position,
            elapsedMs);
        log.Debug(" {Identifier} Processed {@Position} in {Elapsed:000} ms. ", identifier, position, elapsedMs);
        log.Verbose(" {Identifier} Processed {@Position} in {Elapsed:000} ms.", identifier, position, elapsedMs);
        log.Information(" {Identifier} Processed {@Position} in {Elapsed:000} ms.", identifier, position, elapsedMs);
        log.Warning(" {Identifier} Processed {@Position} in {Elapsed:000} ms.", identifier, position, elapsedMs);
        log.Error(" {Identifier} Zohar Processed {@Position} in {Elapsed:000} ms.", identifier, position,
            elapsedMs);
        log.Debug(" {Identifier} Processed {@Position} in {Elapsed:000} ms. ", identifier, position, elapsedMs);

        await Task.Delay(10000);

        if (String.Equals(runMode, "durable"))
        {
            int lineCount = 0;
            foreach (string file in Directory.EnumerateFiles(Directory.GetCurrentDirectory() + "/" + randomInt, "*.clef"))
            {
                lineCount += System.IO.File.ReadLines(file).Count();
            }
            Assert.Equal(result, lineCount);
        }

        int noOfRecordsIngested = GetNoOfRecordsIngestedInAdx(identifier);
        Assert.Equal(result, noOfRecordsIngested);
    }

    private Logger GetSerilogAdxSink(string identifier)
    {
        Logger logger = null;
        switch (identifier)
        {
            case "Test_AzureDataExplorer_Serilog_Sink_Queued_Ingestion_Durable":
                logger = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.AzureDataExplorerSink(new AzureDataExplorerSinkOptions
                    {
                        IngestionEndpointUri = Environment.GetEnvironmentVariable("ingestionURI"),
                        DatabaseName = Environment.GetEnvironmentVariable("databaseName"),
                        BatchPostingLimit = 10,
                        Period = TimeSpan.FromSeconds(5),
                        TableName = m_generatedTableName,
                        BufferBaseFileName = m_bufferBaseFileName,
                        BufferFileRollingInterval = RollingInterval.Minute,
                        FlushImmediately = true,
                        ColumnsMapping = m_columnMappings
                    }.WithAadApplicationKey(Environment.GetEnvironmentVariable("appId"),
                        Environment.GetEnvironmentVariable("appKey"), Environment.GetEnvironmentVariable("tenant")))
                    .CreateLogger();
                break;
            case "Test_AzureDataExplorer_Serilog_Sink_LogLevelSwitch_Durable":
                logger = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.AzureDataExplorerSink(new AzureDataExplorerSinkOptions
                    {
                        IngestionEndpointUri = Environment.GetEnvironmentVariable("ingestionURI"),
                        BatchPostingLimit = 10,
                        Period = TimeSpan.FromMilliseconds(1000),
                        DatabaseName = Environment.GetEnvironmentVariable("databaseName"),
                        TableName = m_generatedTableName,
                        BufferBaseFileName = m_bufferBaseFileName,
                        BufferFileRollingInterval = RollingInterval.Minute,
                        BufferFileLoggingLevelSwitch = new LoggingLevelSwitch(Events.LogEventLevel.Error),
                        FlushImmediately = true,
                        ColumnsMapping = m_columnMappings
                    }.WithAadApplicationKey(Environment.GetEnvironmentVariable("appId"),
                        Environment.GetEnvironmentVariable("appKey"), Environment.GetEnvironmentVariable("tenant")))
                    .CreateLogger();
                break;
            case "Test_AzureDataExplorer_Serilog_Sink_Queued_Ingestion_NonDurable":
                logger = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.AzureDataExplorerSink(new AzureDataExplorerSinkOptions
                    {
                        IngestionEndpointUri = Environment.GetEnvironmentVariable("ingestionURI"),
                        DatabaseName = Environment.GetEnvironmentVariable("databaseName"),
                        BatchPostingLimit = 10,
                        Period = TimeSpan.FromMilliseconds(1000),
                        TableName = m_generatedTableName,
                        FlushImmediately = true,
                        ColumnsMapping = m_columnMappings
                    }.WithAadApplicationKey(Environment.GetEnvironmentVariable("appId"),
                        Environment.GetEnvironmentVariable("appKey"), Environment.GetEnvironmentVariable("tenant")))
                    .CreateLogger();
                break;
            case "Test_AzureDataExplorer_Serilog_Sink_With_Streaming_NonDurable":
                logger = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.AzureDataExplorerSink(new AzureDataExplorerSinkOptions
                    {
                        IngestionEndpointUri = Environment.GetEnvironmentVariable("ingestionURI"),
                        DatabaseName = Environment.GetEnvironmentVariable("databaseName"),
                        BatchPostingLimit = 10,
                        Period = TimeSpan.FromMilliseconds(1000),
                        TableName = m_generatedTableName,
                        UseStreamingIngestion = true,
                        FlushImmediately = true,
                        ColumnsMapping = m_columnMappings
                    }.WithAadApplicationKey(Environment.GetEnvironmentVariable("appId"),
                        Environment.GetEnvironmentVariable("appKey"), Environment.GetEnvironmentVariable("tenant")))
                    .CreateLogger();
                break;
        }
        return logger;
    }

    private int GetNoOfRecordsIngestedInAdx(string searchString)
    {
        var noOfRecordsIngested = 0;
        using var queryProvider = KustoClientFactory.CreateCslQueryProvider(m_kustoConnectionStringBuilder);
        string query = $"{m_generatedTableName} | where Message contains '{searchString}' | count ";
        var clientRequestProperties = new ClientRequestProperties()
        {
            ClientRequestId = Guid.NewGuid().ToString()
        };
        using (var reader = queryProvider.ExecuteQuery(Environment.GetEnvironmentVariable("databaseName"), query,
                   clientRequestProperties))
        {
            // Read HowManyRecords
            while (reader.Read())
            {
                noOfRecordsIngested = (int)reader.GetInt64(0);
            }
        }

        return noOfRecordsIngested;
    }

    public void Dispose()
    {
        using (var queryProvider = KustoClientFactory.CreateCslAdminProvider(m_kustoConnectionStringBuilder))
        {
            var command = CslCommandGenerator.GenerateTableDropCommand(m_generatedTableName);
            var clientRequestProperties = new ClientRequestProperties()
            {
                ClientRequestId = Guid.NewGuid().ToString()
            };
            queryProvider.ExecuteControlCommand(Environment.GetEnvironmentVariable("databaseName"), command,
                clientRequestProperties);
        }
    }
}
