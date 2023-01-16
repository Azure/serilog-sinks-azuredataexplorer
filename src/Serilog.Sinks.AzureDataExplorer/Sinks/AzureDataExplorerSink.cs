using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using Kusto.Data.Common;
using Kusto.Ingest;
using Microsoft.IO;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.AzureDataExplorer.Extensions;
using Serilog.Sinks.PeriodicBatching;

[assembly: InternalsVisibleTo("Serilog.Sinks.AzureDataExplorer.Tests")]
namespace Serilog.Sinks.AzureDataExplorer.Sinks
{
    internal class AzureDataExplorerSink : IBatchedLogEventSink, IDisposable
    {
        private static readonly RecyclableMemoryStreamManager SRecyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        private static readonly List<ColumnMapping> SDefaultIngestionColumnMapping = new List<ColumnMapping>
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
        private readonly Logger m_sink;
        private readonly string m_mappingName;
        private readonly bool m_flushImmediately;
        private readonly bool m_streamingIngestion;
        private readonly bool m_durableMode;
        private readonly IngestionMapping m_ingestionMapping;
        private IKustoIngestClient m_ingestClient;
        private IKustoQueuedIngestClient m_kustoQueuedIngestClient;
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
            m_flushImmediately = options.FlushImmediately;
            m_streamingIngestion = options.UseStreamingIngestion;

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
                m_ingestionMapping.IngestionMappings = SDefaultIngestionColumnMapping;
            }

            if (!string.IsNullOrEmpty(options.BufferFileName))
            {
                Path.GetFullPath(options.BufferFileName); // validate path
                m_durableMode = true;
                m_sink = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.File(options.BufferFileName,
                        outputTemplate: options.BufferFileOutputFormat,
                        rollingInterval: options.BufferFileRollingInterval,
                        fileSizeLimitBytes: options.BufferFileSizeLimitBytes,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: options.BufferFileCountLimit,
                        levelSwitch: options.BufferFileLoggingLevelSwitch,
                        encoding: Encoding.UTF8)
                    .CreateLogger();
            }

            var kcsb = options.GetKustoConnectionStringBuilder();
            var engineKcsb = options.GetKustoEngineConnectionStringBuilder();

            if (options.UseStreamingIngestion)
            {
                m_ingestClient = KustoIngestFactory.CreateManagedStreamingIngestClient(engineKcsb, kcsb);
            }
            else
            {
                m_kustoQueuedIngestClient = KustoIngestFactory.CreateQueuedIngestClient(kcsb);
            }
        }

        public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
        {
            if (m_durableMode)
            {
                foreach (var logEvent in batch)
                {
                    m_sink.Write(logEvent);
                }
            }
            using (var dataStream = CreateStreamFromLogEvents(batch))
            {
                if (!m_streamingIngestion)
                {
                    await m_kustoQueuedIngestClient.IngestFromStreamAsync(
                        dataStream,
                        new KustoQueuedIngestionProperties(m_databaseName, m_tableName)
                        {
                            DatabaseName = m_databaseName,
                            TableName = m_tableName,
                            FlushImmediately = m_flushImmediately,
                            Format = DataSourceFormat.multijson,
                            IngestionMapping = m_ingestionMapping
                        },
                        new StreamSourceOptions
                        {
                            LeaveOpen = false,
                            CompressionType = DataSourceCompressionType.GZip
                        }).ConfigureAwait(false);
                }
                else
                {
                    await m_ingestClient.IngestFromStreamAsync(
                        dataStream,
                        new KustoIngestionProperties()
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
        }

        public Task OnEmptyBatchAsync()
        {
            return Task.CompletedTask;
        }

        private Stream CreateStreamFromLogEvents(IEnumerable<LogEvent> batch)
        {
            var stream = SRecyclableMemoryStreamManager.GetStream();
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
                m_ingestClient?.Dispose();
                m_ingestClient = null;
                
                m_kustoQueuedIngestClient?.Dispose();
                m_kustoQueuedIngestClient = null;
            }

            m_disposed = true;
        }
        #endregion
    }
}
