﻿// Copyright 2014 Serilog Contributors
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

using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Kusto.Data.Common;
using Kusto.Data.Exceptions;
using Kusto.Ingest;
using Kusto.Ingest.Exceptions;
using Microsoft.IO;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.AzureDataExplorer.Extensions;

[assembly: InternalsVisibleTo("Serilog.Sinks.AzureDataExplorer.Tests,PublicKey=" + "002400000480000094000000060200000024000052534131000400000100010025d2229d740f195c0a4cdcb468a4ed69c33a9f2738727a6c34a80ab8b75263a33bd5ac958f0e8b82658a7ee429cc4536166a7ac908691c600a84b20a67db8f5324f43a168a93665f6b449588d2168d6189a27f41bf7b95e6cd1f184bf6f9f9020429972e3132f34f60777ff25edd96d0527d88d2adb4dffa4ed31016aa6cc5b0")]

namespace Serilog.Sinks.AzureDataExplorer.Sinks
{
    internal sealed class AzureDataExplorerSink : IBatchedLogEventSink, IDisposable
    {
        private static readonly JsonSerializerOptions options = new JsonSerializerOptions()
        {
            Converters =
            {
                new ExceptionsJsonConverter<Exception>()
            }
        };
        private static readonly RecyclableMemoryStreamManager SRecyclableMemoryStreamManager = new RecyclableMemoryStreamManager();

        private static readonly List<ColumnMapping> SDefaultIngestionColumnMapping = new List<ColumnMapping>
        {
            new ColumnMapping
            {
                ColumnName = "Timestamp",
                ColumnType = "datetime",
                Properties = new Dictionary<string, string>
                {
                    {
                        MappingConsts.Path, "$.Timestamp"
                    }
                }
            },
            new ColumnMapping
            {
                ColumnName = "Level",
                ColumnType = "string",
                Properties = new Dictionary<string, string>
                {
                    {
                        MappingConsts.Path, "$.Level"
                    }
                }
            },
            new ColumnMapping
            {
                ColumnName = "Message",
                ColumnType = "string",
                Properties = new Dictionary<string, string>
                {
                    {
                        MappingConsts.Path, "$.Message"
                    }
                }
            },
            new ColumnMapping
            {
                ColumnName = "Exception",
                ColumnType = "string",
                Properties = new Dictionary<string, string>
                {
                    {
                        MappingConsts.Path, "$.Exception"
                    }
                }
            },
            new ColumnMapping
            {
                ColumnName = "Properties",
                ColumnType = "dynamic",
                Properties = new Dictionary<string, string>
                {
                    {
                        MappingConsts.Path, "$.Properties"
                    }
                }
            },
        };

        private readonly IFormatProvider m_formatProvider;
        private readonly string m_databaseName;
        private readonly string m_tableName;

        private readonly bool m_flushImmediately;
        private readonly bool m_streamingIngestion;
        private readonly IngestionMapping m_ingestionMapping;
        private IKustoIngestClient m_ingestClient;
        private bool m_disposed;

        private AzureDataExplorerSinkOptions m_options;

        public AzureDataExplorerSink(AzureDataExplorerSinkOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            m_options = options;
            m_databaseName = options.DatabaseName ?? throw new ArgumentNullException(nameof(options.DatabaseName));
            m_tableName = options.TableName ?? throw new ArgumentNullException(nameof(options.TableName));
            if (options.IngestionEndpointUri == null) throw new ArgumentNullException(nameof(options.IngestionEndpointUri));
            m_formatProvider = options.FormatProvider;
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
                m_ingestionMapping.IngestionMappings = options.ColumnsMapping.Select(m => new ColumnMapping
                {
                    ColumnName = m.ColumnName,
                    ColumnType = m.ColumnType,
                    Properties = new Dictionary<string, string>(1)
                    {
                        {
                            MappingConsts.Path, m.ValuePath
                        }
                    }
                }).ToList();
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
            await EmitBatchAsync(batch.ToList());
        }

        public async Task EmitBatchAsync(IReadOnlyCollection<LogEvent> batch)
        {
            using var dataStream = CreateStreamFromLogEvents(batch);
            for (int retry = 1; retry <= m_options.IngestionRetries; ++retry)
            {
                try
                {
                    var sourceId = Guid.NewGuid();
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
                                SourceId = sourceId,
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
                                SourceId = sourceId,
                                LeaveOpen = false,
                                CompressionType = DataSourceCompressionType.GZip
                            }).ConfigureAwait(false);
                    }
                }
                catch (KustoClientApplicationAuthenticationException ex)
                {
                    if (m_options.FailOnError)
                    {
                        SelfLog.WriteLine("Auth failure on  Kusto sink due to authentication error (EmitBatchAsync). Please check your credentials", ex);
                        SelfLog.WriteLine("FailOnError is set to true, the exception will be thrown to the caller to handle the failure");
                        throw new LoggingFailedException($"Auth failure on  Kusto sink due to authentication error (EmitBatchAsync). Please check your credentials.{ex.Message}");
                    }
                    SelfLog.WriteLine("Auth failure on  Kusto sink due to authentication error (EmitBatchAsync). Please check your credentials.", ex);

                }
                catch (IngestClientException ex)
                {
                    if (ex.IsPermanent)
                    { // <- no retry
                        if (m_options.FailOnError)
                        {
                            SelfLog.WriteLine("Permanent ingestion failure ingesting to Kusto.FailOnError is set to true, the exception will be thrown to the caller to handle the failure", ex);
                            throw new LoggingFailedException($"Permanent ingestion failure ingesting to Kusto.{ex.Message}");
                        }
                        // Since the failure is permanent, we can't retry, so we'll just log the error and continue.
                        SelfLog.WriteLine("Permanent ingestion failure ingesting to Kusto.Since FailOnError is not set, the batch will be dropped", ex);
                        break; // no more retries on unwanted exception
                    }
                    else
                    {
                        SelfLog.WriteLine($"Temporary failures writing to Kusto (Retry attempt {retry} of {m_options.IngestionRetries})", ex);
                    }
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
                        System.Text.Json.JsonSerializer.Serialize(compressionStream, logEvent.Dictionary(m_formatProvider), options);
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
