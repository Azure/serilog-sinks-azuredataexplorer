using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.AzureDataExplorer.Sinks;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.AzureDataExplorer.Extensions
{
    //The class "AzureDataExplorerSinkExtensions" is an extension method for the Serilog logging library.
    //It extends the "LoggerSinkConfiguration" class by adding a new method "AzureDataExplorerSink" to it.
    //This method configures the logging pipeline to include the Azure Data Explorer sink and can be used to send log events to Azure Data Explorer.
    //The method sets up batching for the log events and enables configurable options such as the batch size, posting limit, and event queue size limit.
    //If a buffer base file name is provided, the method will use the "AzureDataExplorerDurableSink" class to buffer log events to disk.
    public static class AzureDataExplorerSinkExtensions
    {
        public static LoggerConfiguration AzureDataExplorerSink(
            this LoggerSinkConfiguration loggerConfiguration,
            AzureDataExplorerSinkOptions options,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            if (loggerConfiguration == null)
            {
                throw new ArgumentNullException(nameof(loggerConfiguration));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var batchingOptions = new PeriodicBatchingSinkOptions
            {
                BatchSizeLimit = options.BatchPostingLimit,
                Period = options.Period,
                EagerlyEmitFirstEvent = true,
                QueueLimit = options.QueueSizeLimit
            };

            var azureDataExplorerSink = new AzureDataExplorerSink(options);
            var batchingSink = new PeriodicBatchingSink(azureDataExplorerSink, batchingOptions);

            var sink = string.IsNullOrWhiteSpace(options.BufferBaseFileName) ? (ILogEventSink)batchingSink : new AzureDataExplorerDurableSink(options);
            return loggerConfiguration.Sink(sink,
                restrictedToMinimumLevel,
                options.BufferFileLoggingLevelSwitch);
        }
    }
}
