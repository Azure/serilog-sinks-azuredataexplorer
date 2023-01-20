﻿using System.IO.Compression;
using System.Runtime.CompilerServices;
using Kusto.Data.Common;
using Kusto.Ingest;
using Microsoft.IO;
using Serilog.Events;
using Serilog.Sinks.AzureDataExplorer.Extensions;
using Serilog.Sinks.PeriodicBatching;

[assembly: InternalsVisibleTo("Serilog.Sinks.AzureDataExplorer.Tests")]
namespace Serilog.Sinks.AzureDataExplorer.Sinks
{
    internal sealed class AzureDataExplorerSink : IBatchedLogEventSink, IDisposable
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

        private readonly bool m_flushImmediately;
        private readonly bool m_streamingIngestion;
        private readonly IngestionMapping m_ingestionMapping;
        private IKustoIngestClient m_ingestClient;
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
            var mappingName = options.MappingName;
            m_flushImmediately = options.FlushImmediately;
            m_streamingIngestion = options.UseStreamingIngestion;

            m_ingestionMapping = new IngestionMapping();
            if (!string.IsNullOrEmpty(mappingName))
            {
                m_ingestionMapping.IngestionMappingReference = mappingName;
            }
            else if (options.ColumnsMapping?.Any() == true)
            {
                m_ingestionMapping.IngestionMappings = options.ColumnsMapping.Select(m => new ColumnMapping { ColumnName = m.ColumnName, ColumnType = m.ColumnType, Properties = new Dictionary<string, string>(1) { { MappingConsts.Path, m.ValuePath } } }).ToList();
            }
            else
            {
                m_ingestionMapping.IngestionMappings = SDefaultIngestionColumnMapping;
            }

            var kcsb = options.GetKustoConnectionStringBuilder();
            var engineKcsb = options.GetKustoEngineConnectionStringBuilder();

            if (options.UseStreamingIngestion)
            {
                m_ingestClient = KustoIngestFactory.CreateManagedStreamingIngestClient(engineKcsb, kcsb);
            }
            else
            {
                m_ingestClient = KustoIngestFactory.CreateQueuedIngestClient(kcsb);
            }
        }

        public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
        {
            using (var dataStream = CreateStreamFromLogEvents(batch))
            {
                if (!m_streamingIngestion)
                {
                    await m_ingestClient.IngestFromStreamAsync(
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

        private void Dispose(bool disposing)
        {
            if (m_disposed)
            {
                return;
            }

            if (disposing)
            {
                m_ingestClient?.Dispose();
                m_ingestClient = null;
            }

            m_disposed = true;
        }
        #endregion
    }
}
