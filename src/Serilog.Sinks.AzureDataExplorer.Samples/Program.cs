using Serilog.Sinks.Azuredataexplorer;

namespace Serilog.Sinks.AzureDataExplorer.Samples
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
                    IngestionEndpointUri = "<ADXIngestionURL>",
                    DatabaseName = "<databaseName>",
                    TableName = "<tableName>",
                    bufferFileName = "<bufferFileName>",

                    ColumnsMapping = new[]
                    {
                        new SinkColumnMapping { ColumnName ="Timestamp", ColumnType ="datetime", ValuePath = "$.Timestamp" } ,
                        new SinkColumnMapping { ColumnName ="Level", ColumnType ="string", ValuePath = "$.Level" } ,
                        new SinkColumnMapping { ColumnName ="Message", ColumnType ="string", ValuePath = "$.Message" } ,
                        new SinkColumnMapping { ColumnName ="Exception", ColumnType ="string", ValuePath = "$.Exception" } ,
                        new SinkColumnMapping { ColumnName ="Properties", ColumnType ="dynamic", ValuePath = "$.Properties" } ,
                        new SinkColumnMapping { ColumnName ="Position", ColumnType ="dynamic", ValuePath = "$.Properties.Position" } ,
                        new SinkColumnMapping { ColumnName ="Elapsed", ColumnType ="int", ValuePath = "$.Properties.Elapsed" } ,
                    }
                }.WithAadApplicationKey("<appId>", "<appKey>", "<tenant>"))
                .CreateLogger();

            var position = new { Latitude = 25, Longitude = 134 };
            var elapsedMs = 34;

            log.Verbose("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Information("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Warning("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Error(new Exception(), "Zohar Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            log.Debug("Processed {@Position} in {Elapsed:000} ms. ", position, elapsedMs);
        }
    }
}