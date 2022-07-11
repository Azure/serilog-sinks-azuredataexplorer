using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Azuredataexplorer;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.AzureDataExplorer
{
    public static class AzureDataExplorerSinkExtensions
    {
        public static LoggerConfiguration AzureDataExplorerSink(
            this LoggerSinkConfiguration loggerConfiguration,
            AzureDataExplorerSinkOptions options,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            LoggingLevelSwitch levelSwitch = null)
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
            return loggerConfiguration.Sink(batchingSink,
                restrictedToMinimumLevel,
                levelSwitch);
        }
    }
}