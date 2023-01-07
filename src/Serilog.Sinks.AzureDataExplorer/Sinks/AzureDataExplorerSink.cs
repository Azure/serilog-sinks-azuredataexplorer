using System.IO.Compression;
using System.Text;
using Kusto.Data.Common;
using Kusto.Ingest;
using Microsoft.IO;
using Serilog.Core;
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
        private readonly Logger m_sink;

        private readonly bool isDurableMode;

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

            if (!string.IsNullOrEmpty(options.bufferFileName))
            {
                Path.GetFullPath(options.bufferFileName);     // validate path
                isDurableMode = true;
                m_sink = new LoggerConfiguration()
                            .MinimumLevel.Verbose()
                            .WriteTo.File(options.bufferFileName,
                                outputTemplate: options.bufferFileOutputFormat,
                                rollingInterval: options.bufferFileRollingInterval,
                                fileSizeLimitBytes: options.bufferFileSizeLimitBytes,
                                rollOnFileSizeLimit: true,
                                retainedFileCountLimit: options.bufferFileCountLimit,
                                levelSwitch: options.bufferFileLoggingLevelSwitch,
                                encoding: Encoding.UTF8)
                            .CreateLogger();
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
            if (isDurableMode)
            {
                foreach (var logEvent in batch)
                {
                    m_sink.Write(logEvent);
                }
            }
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
                        LeaveOpen = false,
                        CompressionType = DataSourceCompressionType.GZip
                    }).ConfigureAwait(false);
            }
        }

        public Task OnEmptyBatchAsync()
        {
            return Task.CompletedTask;
        }

        private Stream CreateStreamFromLogEvents(IEnumerable<LogEvent> batch)
        {
            var stream = s_recyclableMemoryStreamManager.GetStream();
            {
                using (GZipStream compressionStream = new GZipStream(stream, CompressionMode.Compress, leaveOpen: true))
                {
                    foreach (var logEvent in batch)
                    {
                        System.Text.Json.JsonSerializer.Serialize(compressionStream, logEvent.Dictionary(m_formatProvider));
                    }
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