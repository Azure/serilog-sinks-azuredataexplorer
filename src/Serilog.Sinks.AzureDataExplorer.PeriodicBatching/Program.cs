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
using Kusto.Cloud.Platform.Utils;
using Serilog.Sinks.AzureDataExplorer.Extensions;

namespace Serilog.Sinks.AzureDataExplorer.PeriodicBatching
{
    class Program
    {
        static void Main(string[] args)
        {
            var scopes = new List<string> { Environment.GetEnvironmentVariable("ingestionURI") + "/.default" }.ToArray();
            var tokenRequestContext = new TokenRequestContext(scopes, tenantId: Environment.GetEnvironmentVariable("tenant"));
            var m_accessToken = new AzureCliCredential().GetToken(tokenRequestContext).Token;
            
            var log = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.AzureDataExplorerSink(new AzureDataExplorerSinkOptions
                {
                    IngestionEndpointUri = Environment.GetEnvironmentVariable("ingestionURI"),
                    DatabaseName = Environment.GetEnvironmentVariable("databaseName"),
                    TableName = Environment.GetEnvironmentVariable("tableName"),
                    FlushImmediately = Environment.GetEnvironmentVariable("flushImmediately").IsNotNullOrEmpty() && bool.Parse(Environment.GetEnvironmentVariable("flushImmediately")!),
                    //configure the following variables to enable Periodic Batching
                    BufferBaseFileName = Environment.GetEnvironmentVariable("bufferBaseFileName"),
                    BatchPostingLimit = 10, 
                    Period = TimeSpan.FromSeconds(5),

                    ColumnsMapping = new[]
                    {
                        new SinkColumnMapping { ColumnName ="Timestamp", ColumnType ="datetime", ValuePath = "$.Timestamp" },
                        new SinkColumnMapping { ColumnName ="Level", ColumnType ="string", ValuePath = "$.Level" },
                        new SinkColumnMapping { ColumnName ="Message", ColumnType ="string", ValuePath = "$.Message" },
                        new SinkColumnMapping { ColumnName ="Exception", ColumnType ="string", ValuePath = "$.Error" },
                        new SinkColumnMapping { ColumnName ="Properties", ColumnType ="dynamic", ValuePath = "$.Properties" },
                        new SinkColumnMapping { ColumnName ="Position", ColumnType ="dynamic", ValuePath = "$.Properties.Position" },
                        new SinkColumnMapping { ColumnName ="Elapsed", ColumnType ="int", ValuePath = "$.Properties.Elapsed" },
                    }
                }.WithAadUserToken(m_accessToken)).CreateLogger();


            // Define the number of iterations for the logs
            int iterations = 10;

            string[] logLevels = { "Verbose", "Information", "Warning", "Error", "Debug" };
            var position = new { Latitude = 25, Longitude = 134 };
            var elapsedMs = 34;
            var message = $"Processed {{@Position}} in {{Elapsed:000}} ms.";
            for (int i = 1; i <= iterations; i++)
            {
                foreach (var level in logLevels)
                {
                    switch (level)
                    {
                        case "Verbose":
                            log.Verbose(message, position, elapsedMs);
                            break;
                        case "Information":
                            log.Information(message, position, elapsedMs);
                            break;
                        case "Warning":
                            log.Warning(message, position, elapsedMs);
                            break;
                        case "Error":
                            log.Error(new Exception(), message, position, elapsedMs);
                            break;
                        case "Debug":
                            log.Debug(message, position, elapsedMs);
                            break;
                    }
                }
            }

            Thread.Sleep(10000);
        }
    }

}
