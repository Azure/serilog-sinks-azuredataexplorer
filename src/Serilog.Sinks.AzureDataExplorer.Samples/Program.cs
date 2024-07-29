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
                    /*configure the following variables to enable Periodic Batching
                    BatchPostingLimit = 10,
                    Period = TimeSpan.FromSeconds(5),
                    */
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
