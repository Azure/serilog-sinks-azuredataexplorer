using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Ingest;
using Microsoft.IO;
using Newtonsoft.Json;
using Serilog.Events;
using Serilog.Sinks.Azuredataexplorer;
using Serilog.Sinks.Azuredataexplorer.Extensions;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.AzureDataExplorer
{
    internal class AzureDataExplorerSink : IBatchedLogEventSink, IDisposable
    {
        private static readonly RecyclableMemoryStreamManager s_recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        private static readonly List<ColumnMapping> s_defaultIngestionColumnMapping = new List<ColumnMapping>
        {
            new ColumnMapping { ColumnName = "Timestamp", ColumnType = "datetime", Properties = new Dictionary<string, string>{{ MappingConsts.Path, "$.Timestamp" } } },
            new ColumnMapping { ColumnName = "Level", ColumnType = "string", Properties = new Dictionary<string, string>{{ MappingConsts.Path, "$.Level" } } },
            new ColumnMapping { ColumnName = "Message", ColumnType = "string", Properties = new Dictionary<string, string>{{ MappingConsts.Path, "$.Message" } } },
            new ColumnMapping { ColumnName = "Exception", ColumnType = "string", Properties = new Dictionary<string, string>{{ MappingConsts.Path, "$.Exception" } } },
            new ColumnMapping { ColumnName = "Properties", ColumnType = "dynamic", Properties = new Dictionary<string, string>{{ MappingConsts.Path, "$.Properties" } } },
        };

        private readonly IFormatProvider m_formatProvider;
        private readonly string m_databaseName;
        private readonly string m_tableName;
        private readonly string m_mappingName;

        private readonly IngestionMapping m_ingestionMapping;
        //private KustoIngestionProperties m_kustoIngestionProperties;
        //private StreamSourceOptions m_streamSourceOptions;

        private IKustoIngestClient m_queuedIngestClient;

        private bool m_disposed;

        public AzureDataExplorerSink(AzureDataExplorerSinkOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (options.DatabaseName == null)
            {
                throw new ArgumentNullException(nameof(options.DatabaseName));
            }
            if (options.TableName == null)
            {
                throw new ArgumentNullException(nameof(options.TableName));
            }
            if (options.IngestionEndpointUri == null)
            {
                throw new ArgumentNullException(nameof(options.IngestionEndpointUri));
            }

            m_formatProvider = options.FormatProvider;
            m_databaseName = options.DatabaseName;
            m_tableName = options.TableName;
            m_mappingName = options.MappingName;

            m_ingestionMapping = new IngestionMapping();
            if (!string.IsNullOrEmpty(m_mappingName))
            {
                m_ingestionMapping.IngestionMappingReference = m_mappingName;
            }
            else if (options.ColumnsMapping?.Any() == true)
            {
                m_ingestionMapping.IngestionMappings = options.ColumnsMapping.Select(m => new ColumnMapping { ColumnName = m.ColumnName, ColumnType = m.ColumnType, Properties = new Dictionary<string, string>(1) { { MappingConsts.Path, m.ValuePath } } }).ToList();
            }
            else
            {
                m_ingestionMapping.IngestionMappings = s_defaultIngestionColumnMapping;
            }

            var kcsb = options.GetKustoConnectionStringBuilder();

            if (options.UseStreamingIngestion)
            {
                m_queuedIngestClient = KustoIngestFactory.CreateStreamingIngestClient(kcsb);
            }
            else
            {
                m_queuedIngestClient = KustoIngestFactory.CreateQueuedIngestClient(kcsb);
            }
        }

        public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
        {
            using (var dataStream = CreateStreamFromLogEvents(batch))
            {
                var result = await m_queuedIngestClient.IngestFromStreamAsync(
                    dataStream,
                    new KustoIngestionProperties
                    {
                        DatabaseName = m_databaseName,
                        TableName = m_tableName,
                        Format = DataSourceFormat.multijson,
                        IngestionMapping = m_ingestionMapping
                    },
                    new StreamSourceOptions
                    {
                        LeaveOpen = false
                    }).ConfigureAwait(false);

                //IEnumerable<IngestionStatus>? status;

                //do
                //{
                //    status = result.GetIngestionStatusCollection();
                //    await Task.Delay(5000).ConfigureAwait(false);
                //}
                //while (status.First().Status == Status.Queued);
            }
        }

        public Task OnEmptyBatchAsync()
        {
            return Task.CompletedTask;
        }

        //private void CreateJsonMappingIfNotExists()
        //{
        //    var kcsb = new KustoConnectionStringBuilder(m_queryEndpointUri, m_databaseName)
        //        .WithAadUserPromptAuthentication();
        //    using (var adminClient = KustoClientFactory.CreateCslAdminProvider(kcsb))
        //    {
        //        var showMappingsCommand = CslCommandGenerator.GenerateTableJsonMappingsShowCommand(m_tableName);
        //        var existingMappings = adminClient.ExecuteControlCommand<IngestionMappingShowCommandResult>(showMappingsCommand);

        //        if (existingMappings.Any(m => string.Equals(m.Name, m_mappingName, StringComparison.Ordinal)))
        //        {
        //            return;
        //        }

        //        var createMappingCommand = CslCommandGenerator.GenerateTableMappingCreateCommand(Kusto.Data.Ingestion.IngestionMappingKind.Json, m_tableName, m_mappingName, s_defaultIngestionColumnMapping);
        //        adminClient.ExecuteControlCommand(m_databaseName, createMappingCommand);
        //    }
        //}

        private Stream CreateStreamFromLogEvents(IEnumerable<LogEvent> batch)
        {
            var stream = s_recyclableMemoryStreamManager.GetStream();
            //using (StreamWriter textWriter = new StreamWriter(stream, encoding, bufferSize, leaveOpen))
            //using (JsonTextWriter jsonWriter = new JsonTextWriter(textWriter))
            {
                foreach (var logEvent in batch)
                {
                    System.Text.Json.JsonSerializer.Serialize(stream, logEvent.Dictionary(m_formatProvider));
                }
            }

            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        #region IDisposable methods
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_disposed)
            {
                return;
            }

            if (disposing)
            {
                m_queuedIngestClient?.Dispose();
                m_queuedIngestClient = null;
            }

            m_disposed = true;
        }
        #endregion
    }
}