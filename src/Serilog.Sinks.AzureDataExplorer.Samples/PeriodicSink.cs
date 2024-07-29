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

using Azure.Core;
using Azure.Identity;
using Kusto.Cloud.Platform.Security;
using Kusto.Cloud.Platform.Utils;
using Serilog.Configuration;
using Serilog.Sinks.AzureDataExplorer.Extensions;
using Serilog.Sinks.AzureDataExplorer.Sinks;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.AzureDataExplorer
{

    /*
    export ingestionURI="https://ingest-sdktestcluster.southeastasia.dev.kusto.windows.net/"
    export databaseName="e2e"
    export tableName="Serilog"

    */
    public class PeriodicSink
    {
        public static void Main(string[] args)
        {
            var applicationToken = new DefaultAzureCredential().GetToken(new TokenRequestContext(new[] { "https://kusto.kusto.windows.net/.default" })).Token;
            var adxSinkOptions = new AzureDataExplorerSinkOptions
            {
                IngestionEndpointUri = Environment.GetEnvironmentVariable("ingestionURI"),
                DatabaseName = Environment.GetEnvironmentVariable("databaseName"),
                TableName = Environment.GetEnvironmentVariable("tableName"),
            }.WithAadApplicationToken(applicationToken);

            var adxSink = new AzureDataExplorerSink(adxSinkOptions);

            var batchingOptions = new PeriodicBatchingSinkOptions
            {
                BatchSizeLimit = 100,
                Period = TimeSpan.FromSeconds(5),
                EagerlyEmitFirstEvent = true,
                QueueLimit = 10000
            };

            var batchingSink = new PeriodicBatchingSink((IBatchedLogEventSink)adxSink, batchingOptions);

            var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Sink(batchingSink).CreateLogger();
            for (int i = 0; i < 1000; i++)
            {
                var position = new { Latitude = 25, Longitude = 134 };
                var elapsedMs = 34;

                log.Verbose("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
                log.Information("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
                log.Warning("Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
                log.Error(new Exception(), "Sample Processed {@Position} in {Elapsed:000} ms.", position, elapsedMs);
                log.Debug("Processed {@Position} in {Elapsed:000} ms. ", position, elapsedMs);
            }
            Thread.Sleep(10000);
        }
    }
}