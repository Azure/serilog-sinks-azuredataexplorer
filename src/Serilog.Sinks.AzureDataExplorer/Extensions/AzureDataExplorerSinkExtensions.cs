// Copyright 2014 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Serilog.Configuration;
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
    }
}
