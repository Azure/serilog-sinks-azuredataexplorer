using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using Kusto.Data.Common;
using Kusto.Ingest;
using Microsoft.IO;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.AzureDataExplorer.Extensions;
using IOFile = System.IO.File;

[assembly: InternalsVisibleTo("Serilog.Sinks.AzureDataExplorer.Tests")]
namespace Serilog.Sinks.AzureDataExplorer.Durable
{
    /// <summary>
    /// Reads and sends logdata to log server
    /// Generic version of  https://github.com/serilog/serilog-sinks-seq/blob/v4.0.0/src/Serilog.Sinks.Seq/Sinks/Seq/Durable/HttpLogShipper.cs
    /// </summary>
    /// <typeparam name="TPayload"></typeparam>
    public sealed class LogShipper<TPayload> : IDisposable
    {
        private static readonly TimeSpan RequiredLevelCheckInterval = TimeSpan.FromSeconds(10);

        private readonly int m_batchPostingLimit;
        private readonly long? m_eventBodyLimitBytes;
        private readonly IPayloadReader<TPayload> m_payloadReader;
        readonly FileSet m_fileSet;
        private readonly long? m_bufferSizeLimitBytes;

        // Timer thread only

        /// <summary>
        /// 
        /// </summary>
        private readonly ExponentialBackoffConnectionSchedule m_connectionSchedule;
        DateTime m_nextRequiredLevelCheckUtc = DateTime.UtcNow.Add(RequiredLevelCheckInterval);

        // Synchronized
        readonly object m_stateLock = new object();

        private readonly PortableTimer m_timer;

        // Concurrent
        readonly ControlledLevelSwitch m_controlledSwitch;

        volatile bool m_unloading;

        private readonly IKustoQueuedIngestClient m_ingestClient;

        private readonly IFormatProvider m_formatProvider;
        private readonly string m_databaseName;
        private readonly string m_tableName;
        private readonly IngestionMapping m_ingestionMapping;
        private readonly bool m_flushImmediately;

        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(300);

        private static readonly TimeSpan TimeBetweenChecks = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bufferBaseFilename"></param>
        /// <param name="batchPostingLimit"></param>
        /// <param name="period"></param>
        /// <param name="eventBodyLimitBytes"></param>
        /// <param name="levelControlSwitch"></param>
        /// <param name="payloadReader"></param>
        /// <param name="bufferSizeLimitBytes"></param>
        /// <param name="ingestClient"></param>
        /// <param name="flushImmediately"></param>
        /// <param name="rollingInterval"></param>
        /// <param name="formatProvider"></param>
        /// <param name="databaseName"></param>
        /// <param name="tableName"></param>
        /// <param name="ingestionMapping"></param>
        public LogShipper(            
            string bufferBaseFilename,            
            int batchPostingLimit,
            TimeSpan period,
            long? eventBodyLimitBytes,
            LoggingLevelSwitch levelControlSwitch,
            IPayloadReader<TPayload> payloadReader,
            long? bufferSizeLimitBytes,
            IKustoQueuedIngestClient ingestClient,
            IFormatProvider formatProvider,
            string databaseName,
            string tableName,
            IngestionMapping ingestionMapping,
            bool flushImmediately,
            RollingInterval rollingInterval = RollingInterval.Hour)
        {
            m_batchPostingLimit = batchPostingLimit;
            m_eventBodyLimitBytes = eventBodyLimitBytes;
            m_payloadReader = payloadReader;
            m_controlledSwitch = new ControlledLevelSwitch(levelControlSwitch);
            m_connectionSchedule = new ExponentialBackoffConnectionSchedule(period);
            m_bufferSizeLimitBytes = bufferSizeLimitBytes;
            m_fileSet = new FileSet(bufferBaseFilename, rollingInterval);
            m_timer = new PortableTimer(c => OnTick());
            m_ingestClient = ingestClient;
            m_formatProvider = formatProvider;
            m_databaseName = databaseName;
            m_tableName = tableName;
            m_ingestionMapping = ingestionMapping;
            m_flushImmediately = flushImmediately;
            SetTimer();

        }

        void CloseAndFlush()
        {
            lock (m_stateLock)
            {
                if (m_unloading)
                    return;

                m_unloading = true;
            }

            m_timer.Dispose();

            OnTick().GetAwaiter().GetResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logEvent"></param>
        /// <returns></returns>
        public bool IsIncluded(LogEvent logEvent)
        {
            return m_controlledSwitch.IsIncluded(logEvent);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            CloseAndFlush();
        }

        private void SetTimer()
        {
            // Note, called under _stateLock
            m_timer.Start(m_connectionSchedule.NextInterval);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task OnTick()
        {
            try
            {
                int count;
                do
                {
                    count = 0;
                    using (var bookmarkFile = m_fileSet.OpenBookmarkFile())
                    {
                        var position = bookmarkFile.TryReadBookmark();
                        var files = m_fileSet.GetBufferFiles();

                        if (position.File == null || !IOFile.Exists(position.File))
                        {
                            position = new FileSetPosition(0, files.FirstOrDefault());
                        }

                        TPayload payload;
                        if (position.File == null)
                        {
                            payload = m_payloadReader.GetNoPayload();
                            count = 0;
                        }
                        else
                        {
                            payload = m_payloadReader.ReadPayload(m_batchPostingLimit, m_eventBodyLimitBytes, ref position, ref count,position.File);
                        }

                        var stopWatch = Stopwatch.StartNew();
                        var fileIdentifier = Guid.NewGuid();
                        
                        if (count > 0 || m_controlledSwitch.IsActive && m_nextRequiredLevelCheckUtc < DateTime.UtcNow)
                        {
                            IKustoIngestionResult result;
                            m_nextRequiredLevelCheckUtc = DateTime.UtcNow.Add(RequiredLevelCheckInterval);
                            using (var dataStream = CreateStreamFromLogEvents(payload))
                            {
                                result = await m_ingestClient.IngestFromStreamAsync(
                                        dataStream,
                                        new KustoQueuedIngestionProperties(m_databaseName, m_tableName)
                                        {
                                            DatabaseName = m_databaseName,
                                            TableName = m_tableName,
                                            FlushImmediately = m_flushImmediately,
                                            Format = DataSourceFormat.multijson,
                                            IngestionMapping = m_ingestionMapping,
                                            ReportLevel = IngestionReportLevel.FailuresAndSuccesses,
                                            ReportMethod = IngestionReportMethod.Table
                                        },
                                        new StreamSourceOptions
                                        {
                                            LeaveOpen = false, 
                                            CompressionType = DataSourceCompressionType.GZip,
                                            SourceId = fileIdentifier
                                        }).ConfigureAwait(false);
                            }
                            var ingestionStatus = result.GetIngestionStatusBySourceId(fileIdentifier);

                            while (true) //loop until the record is updated or we timeout
                            {
                                // check if the record is updated
                                if (ingestionStatus.Status != Status.Pending)
                                {
                                    break; // the record is updated, so we can exit the loop!
                                }

                                // check if we have exceeded our timeout
                                if (stopWatch.Elapsed > Timeout)
                                {
                                    break; // break loop if we timed out
                                }

                                // the record isn't updated & we haven't timed out, so the 
                                // loop will repeat. we're worried about querying the DB 
                                // too often, so we add a delay. this will work a lot like 
                                // a timer, but it is async and avoids reentrancy issues.
                                await Task.Delay(TimeBetweenChecks);
                                ingestionStatus = result.GetIngestionStatusBySourceId(fileIdentifier);
                            }

                            if (ingestionStatus.Status == Status.Succeeded)
                            {
                                m_connectionSchedule.MarkSuccess();
                                bookmarkFile.WriteBookmark(position);
                            }
                            else
                            {
                                m_connectionSchedule.MarkFailure();                               
                                if (m_bufferSizeLimitBytes.HasValue)
                                    m_fileSet.CleanUpBufferFiles(m_bufferSizeLimitBytes.Value, 2);

                                break;
                            }
                        }
                        else if (position.File == null)
                        {
                            break;
                        }
                        else
                        {
                            // For whatever reason, there's nothing waiting to send. This means we should try connecting again at the
                            // regular interval, so mark the attempt as successful.
                            m_connectionSchedule.MarkSuccess();

                            // Only advance the bookmark if no other process has the
                            // current file locked, and its length is as we found it.
                            if (files.Length == 2 && files.First() == position.File &&
                                FileIsUnlockedAndUnextended(position))
                            {
                                bookmarkFile.WriteBookmark(new FileSetPosition(0, files[1]));
                            }

                            if (files.Length > 2)
                            {
                                // By this point, we expect writers to have relinquished locks
                                // on the oldest file.
                                IOFile.Delete(files[0]);
                            }
                        }
                    }
                } while (count == m_batchPostingLimit);
            }
            catch (Exception ex)
            {
                m_connectionSchedule.MarkFailure();
                SelfLog.WriteLine("Exception while emitting periodic batch from {0}: {1}", this, ex);

                if (m_bufferSizeLimitBytes.HasValue)
                    m_fileSet.CleanUpBufferFiles(m_bufferSizeLimitBytes.Value, 2);
            }
            finally
            {
                lock (m_stateLock)
                {
                    if (!m_unloading)
                        SetTimer();
                }
            }
        }
        
        void  DumpInvalidPayload(int statusCode,string resultContent, string payload)
        {
            var invalidPayloadFile = m_fileSet.MakeInvalidPayloadFilename(statusCode);            
            SelfLog.WriteLine("ADX ingestion failed with {0}: {1}; dumping payload to {2}", statusCode,
                resultContent, invalidPayloadFile);
            var bytesToWrite = Encoding.UTF8.GetBytes(payload);
            IOFile.WriteAllBytes(invalidPayloadFile, bytesToWrite);
        }

        static bool FileIsUnlockedAndUnextended(FileSetPosition position)
        {
            try
            {
                using (var fileStream = IOFile.Open(position.File, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
                {
                    return fileStream.Length <= position.NextLineStart;
                }
            }
            catch (IOException)
            {
                // Where no HRESULT is available, assume IOExceptions indicate a locked file
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Unexpected exception while testing locked status of {0}: {1}", position.File, ex);
            }

            return false;
        }
        
        private Stream CreateStreamFromLogEvents(TPayload batch)
        {
            List<LogEvent> payloadBatch = (List<LogEvent>)Convert.ChangeType(batch, typeof(List<LogEvent>));
            var stream = new RecyclableMemoryStreamManager().GetStream();
            {
                using (GZipStream compressionStream = new GZipStream(stream, CompressionMode.Compress, leaveOpen: true))
                {
                    foreach (var logEvent in payloadBatch)
                    {
                        System.Text.Json.JsonSerializer.Serialize(compressionStream, logEvent.Dictionary());
                    }
                }
            }

            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
    }
}
