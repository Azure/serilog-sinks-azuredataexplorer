﻿using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.AzureDataExplorer.Sinks;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.AzureDataExplorer.Extensions
{
    /// <summary>
    /// The class "AzureDataExplorerSinkExtensions" is an extension method for the Serilog logging library.
    /// It extends the "LoggerSinkConfiguration" class by adding a new method "AzureDataExplorerSink" to it.
    /// This method configures the logging pipeline to include the Azure Data Explorer sink and can be used to send log events to Azure Data Explorer.
    /// The method sets up batching for the log events and enables configurable options such as the batch size, posting limit, and event queue size limit.
    /// If a buffer base file name is provided, the method will use the "AzureDataExplorerDurableSink" class to buffer log events to disk.
    /// </summary>
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


        public static LoggerConfiguration AzureDataExplorerSink(
            this LoggerSinkConfiguration loggerConfiguration,
            string ingestionUri,
            string databaseName,
            string tableName,
            string applicationClientId,
            string applicationSecret,
            string tenantId,
            bool flushImmediately = true,
            string mappingName = null,
            string bufferBaseFileName = null,
            //int batchPostingLimit,
            //TimeSpan batchPeriod,
            //int queueSizeLimit,
            
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            if (loggerConfiguration == null)
            {
                throw new ArgumentNullException(nameof(loggerConfiguration));
            }
            if (ingestionUri == null)
            {
                throw new ArgumentNullException(nameof(ingestionUri));
            }

            if (databaseName == null)
            {
                throw new ArgumentNullException(nameof(databaseName));
            }

            if (tableName == null)
            {
                throw new ArgumentNullException(nameof(tableName));
            }

            if (applicationClientId == null)
            {
                throw new ArgumentNullException(nameof(applicationClientId));
            }

            if (applicationSecret == null)
            {
                throw new ArgumentNullException(nameof(applicationSecret));
            }

            if (tenantId == null)
            {
                throw new ArgumentNullException(nameof(tenantId));
            }

            var batchingOptions = new PeriodicBatchingSinkOptions
            {
                //BatchSizeLimit = batchPostingLimit,
                //Period = batchPeriod,
                //EagerlyEmitFirstEvent = true,
                //QueueLimit = queueSizeLimit
            };

            AzureDataExplorerSinkOptions options = new AzureDataExplorerSinkOptions()
            {
                IngestionEndpointUri = ingestionUri,
                DatabaseName = databaseName,
                TableName = tableName,
                BufferBaseFileName = bufferBaseFileName,
                FlushImmediately = flushImmediately,
                MappingName = mappingName,

            }.WithAadApplicationKey(applicationClientId: applicationClientId, applicationKey: applicationSecret, authority: tenantId);


            var azureDataExplorerSink = new AzureDataExplorerSink(options);
            var batchingSink = new PeriodicBatchingSink(azureDataExplorerSink, batchingOptions);

            var sink = string.IsNullOrWhiteSpace(bufferBaseFileName) ? (ILogEventSink)batchingSink : new AzureDataExplorerDurableSink(options);
            return loggerConfiguration.Sink(sink,
                restrictedToMinimumLevel,
                options.BufferFileLoggingLevelSwitch);
        }
    }
}
