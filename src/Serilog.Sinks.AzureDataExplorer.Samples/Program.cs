using Kusto.Cloud.Platform.Utils;
using Serilog.Sinks.AzureDataExplorer.Extensions;

namespace Serilog.Sinks.AzureDataExplorer
{

    class Program
    {
        static void Main(string[] args)
        {
            var log = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.AzureDataExplorerSink(new AzureDataExplorerSinkOptions
                {
                    IngestionEndpointUri = Environment.GetEnvironmentVariable("ingestionURI"),
                    DatabaseName = Environment.GetEnvironmentVariable("databaseName"),
                    TableName = Environment.GetEnvironmentVariable("tableName"),
                    FlushImmediately = Environment.GetEnvironmentVariable("flushImmediately").IsNotNullOrEmpty() && bool.Parse(Environment.GetEnvironmentVariable("flushImmediately")!),
                    BufferBaseFileName = Environment.GetEnvironmentVariable("bufferBaseFileName"),
                    BatchPostingLimit = 10,
                    Period = TimeSpan.FromSeconds(5),

                    ColumnsMapping = new[]
                    {
                        new SinkColumnMapping { ColumnName ="Timestamp", ColumnType ="datetime", ValuePath = "$.Timestamp" } ,
                        new SinkColumnMapping { ColumnName ="Level", ColumnType ="string", ValuePath = "$.Level" } ,
                        new SinkColumnMapping { ColumnName ="Message", ColumnType ="string", ValuePath = "$.Message" } ,
                        new SinkColumnMapping { ColumnName ="Exception", ColumnType ="string", ValuePath = "$.Error" } ,
                        new SinkColumnMapping { ColumnName ="Properties", ColumnType ="dynamic", ValuePath = "$.Properties" } ,
                        new SinkColumnMapping { ColumnName ="Position", ColumnType ="dynamic", ValuePath = "$.Properties.Position" } ,
                        new SinkColumnMapping { ColumnName ="Elapsed", ColumnType ="int", ValuePath = "$.Properties.Elapsed" } ,
                    }
                }.WithAadApplicationKey(Environment.GetEnvironmentVariable("appId"), Environment.GetEnvironmentVariable("appKey"), Environment.GetEnvironmentVariable("tenant"))).CreateLogger();

            var position = new { Latitude = 25, Longitude = 134 };
            var elapsedMs = 34;

            log.Verbose("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Information("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Warning("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Error(new Exception(), "Zohar Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Debug("Processed {@Position} in {Elapsed:000} ms. ", position, elapsedMs);
            
            log.Verbose("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Information("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Warning("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Error(new Exception(), "Zohar Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Debug("Processed {@Position} in {Elapsed:000} ms. ", position, elapsedMs);
            
            log.Verbose("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Information("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Warning("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Error(new Exception(), "Zohar Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Debug("Processed {@Position} in {Elapsed:000} ms. ", position, elapsedMs);
            
            log.Verbose("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Information("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Warning("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Error(new Exception(), "Zohar Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Debug("Processed {@Position} in {Elapsed:000} ms. ", position, elapsedMs);
            
            Thread.Sleep(10000);
        }
    }
}
