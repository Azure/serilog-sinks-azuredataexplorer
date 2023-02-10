using System.Text;
using Kusto.Data.Common;
using Kusto.Ingest;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.AzureDataExplorer.Durable;
using Serilog.Sinks.AzureDataExplorer.Extensions;

namespace Serilog.Sinks.AzureDataExplorer.Sinks
{
    internal class AzureDataExplorerDurableSink : ILogEventSink, IDisposable
    {
        const string FileNameSuffix = "-.clef";

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

        private readonly Logger m_sink;

        private readonly LogShipper<List<LogEvent>> m_shipper;
        private bool m_disposed;
        private IKustoQueuedIngestClient m_kustoQueuedIngestClient;

        public AzureDataExplorerDurableSink(AzureDataExplorerSinkOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            var databaseName = options.DatabaseName ?? throw new ArgumentNullException(nameof(options.DatabaseName));
            var tableName = options.TableName ?? throw new ArgumentNullException(nameof(options.TableName));
            if (options.IngestionEndpointUri == null) throw new ArgumentNullException(nameof(options.IngestionEndpointUri));
            if (string.IsNullOrWhiteSpace(options.BufferBaseFileName))
                throw new ArgumentException("Cannot create the durable ADX sink without a buffer base file name!");

            var flushImmediately = options.FlushImmediately;
            var formatProvider = options.FormatProvider;
            var mappingName = options.MappingName;
            var ingestionMapping = new IngestionMapping();
            if (!string.IsNullOrEmpty(mappingName))
            {
                ingestionMapping.IngestionMappingReference = mappingName;
            }
            else if (options.ColumnsMapping?.Any() == true)
            {
                ingestionMapping.IngestionMappings = options.ColumnsMapping.Select(m => new ColumnMapping
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
                ingestionMapping.IngestionMappings = SDefaultIngestionColumnMapping;
            }

            var kcsb = options.GetKustoConnectionStringBuilder();
            m_kustoQueuedIngestClient = KustoIngestFactory.CreateQueuedIngestClient(kcsb);
            m_sink = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(new CompactJsonFormatter(),
                    options.BufferBaseFileName + FileNameSuffix,
                    restrictedToMinimumLevel: LevelAlias.Minimum,
                    fileSizeLimitBytes: options.BufferFileSizeLimitBytes,
                    levelSwitch: options.BufferFileLoggingLevelSwitch,
                    flushToDiskInterval: TimeSpan.FromSeconds(10),
                    rollingInterval: options.BufferFileRollingInterval,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: options.BufferFileCountLimit,
                    encoding: Encoding.UTF8
                ).CreateLogger();

            var payloadReader = new AzureDataExplorerPayloadReader(
                rollingInterval: options.BufferFileRollingInterval);

            m_shipper = new LogShipper<List<LogEvent>>(
                bufferBaseFilename: options.BufferBaseFileName,
                batchPostingLimit: options.BatchPostingLimit,
                period: options.BufferLogShippingInterval ?? TimeSpan.FromSeconds(5),
                eventBodyLimitBytes: options.SingleEventSizePostingLimit,
                levelControlSwitch: options.BufferFileLoggingLevelSwitch,
                payloadReader: payloadReader,
                bufferSizeLimitBytes: options.BufferFileSizeLimitBytes,
                rollingInterval: options.BufferFileRollingInterval,
                ingestClient: m_kustoQueuedIngestClient,
                formatProvider: formatProvider,
                databaseName: databaseName,
                tableName: tableName,
                ingestionMapping: ingestionMapping,
                flushImmediately: flushImmediately
            );
        }

        public void Emit(LogEvent logEvent)
        {
            // This is a lagging indicator, but the network bandwidth usage benefits
            // are worth the ambiguity.
            if (m_shipper.IsIncluded(logEvent))
            {
                m_sink.Write(logEvent);
            }
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
                m_sink.Dispose();
                m_shipper.Dispose();
            }

            m_disposed = true;
        }

        #endregion
    }
}
