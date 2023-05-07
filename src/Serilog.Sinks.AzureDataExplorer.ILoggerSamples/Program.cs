using Kusto.Cloud.Platform.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Sinks.AzureDataExplorer.Extensions;

namespace Serilog.Sinks.AzureDataExplorer.ILoggerSamples
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Verbose()
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

                   TableNameMappings = new Dictionary<string, string>
                    {
                        { typeof(AnotherClass).FullName ?? string.Empty, "AnotherLogs" }
                    },

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


            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var anotherClass = serviceProvider.GetService<AnotherClass>();
            anotherClass?.DoSomething();

            var logger = serviceProvider.GetService<Microsoft.Extensions.Logging.ILogger<Program>>();
            var position = new { Latitude = 25, Longitude = 134 };
            var elapsedMs = 34;

            logger?.LogTrace("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            logger?.LogInformation("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            logger?.LogWarning("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            logger?.LogError(new Exception(), "Zohar Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            logger?.LogDebug("Processed {@Position} in {Elapsed:000} ms. ", position, elapsedMs);

            logger?.LogTrace("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            logger?.LogInformation("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            logger?.LogWarning("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            logger?.LogError(new Exception(), "Zohar Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            logger?.LogDebug("Processed {@Position} in {Elapsed:000} ms. ", position, elapsedMs);

            logger?.LogTrace("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            logger?.LogInformation("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            logger?.LogWarning("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            logger?.LogError(new Exception(), "Zohar Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            logger?.LogDebug("Processed {@Position} in {Elapsed:000} ms. ", position, elapsedMs);

            logger?.LogTrace("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            logger?.LogInformation("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            logger?.LogWarning("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            logger?.LogError(new Exception(), "Zohar Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
            logger?.LogDebug("Processed {@Position} in {Elapsed:000} ms. ", position, elapsedMs);

            Thread.Sleep(100000);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => configure.AddSerilog())
                    .AddTransient<AnotherClass>();
        }
    }
}