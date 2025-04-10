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

using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using Kusto.Data.Common;
using Kusto.Data.Exceptions;
using Kusto.Ingest;
using Kusto.Ingest.Exceptions;
using Microsoft.IO;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.AzureDataExplorer.Extensions;
using IOFile = System.IO.File;

[assembly: InternalsVisibleTo("Serilog.Sinks.AzureDataExplorer.Tests,PublicKey=" + "002400000480000094000000060200000024000052534131000400000100010025d2229d740f195c0a4cdcb468a4ed69c33a9f2738727a6c34a80ab8b75263a33bd5ac958f0e8b82658a7ee429cc4536166a7ac908691c600a84b20a67db8f5324f43a168a93665f6b449588d2168d6189a27f41bf7b95e6cd1f184bf6f9f9020429972e3132f34f60777ff25edd96d0527d88d2adb4dffa4ed31016aa6cc5b0")]
namespace Serilog.Sinks.AzureDataExplorer.Durable
{
    /// <summary>
    /// Reads and sends logdata to log server
    /// Generic version of  https://github.com/serilog/serilog-sinks-seq/blob/v4.0.0/src/Serilog.Sinks.Seq/Sinks/Seq/Durable/HttpLogShipper.cs
    /// This class sends log data to a specified destination (likely an instance of IKustoQueuedIngestClient).
    /// The class implements the IDisposable interface, which allows it to be used in a using statement to ensure that it is correctly disposed of when it goes out of scope.
    /// The class has fields to store various configuration options such as the batch posting limit, event body limit, payload reader, buffer size limit, and timer.
    /// The class also includes methods for checking if a log event is included, disposing of the shipper, and a tick method (OnTick) that performs the actual data shipment.
    /// When Dispose is called or the using block that the shipper is in ends, the CloseAndFlush method is called, which sets the unloading flag, disposes the timer, and then performs one final shipment of any remaining data.
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

        private readonly AzureDataExplorerSinkOptions m_options;

        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(300);

        private static readonly TimeSpan TimeBetweenChecks = TimeSpan.FromSeconds(5);

        /// <summary>
        /// constructor which initializes 
        /// </summary>

        public LogShipper(
            AzureDataExplorerSinkOptions options,
            TimeSpan period,
            IPayloadReader<TPayload> payloadReader,
            IKustoQueuedIngestClient ingestClient,
            IngestionMapping ingestionMapping)
        {
            m_options = options;
            m_batchPostingLimit = options.BatchPostingLimit;
            m_eventBodyLimitBytes = options.SingleEventSizePostingLimit;
            m_payloadReader = payloadReader;
            m_controlledSwitch = new ControlledLevelSwitch(options.BufferFileLoggingLevelSwitch);
            m_connectionSchedule = new ExponentialBackoffConnectionSchedule(period);
            m_bufferSizeLimitBytes = options.BufferFileSizeLimitBytes;
            m_fileSet = new FileSet(options.BufferBaseFileName, options.BufferFileRollingInterval);
            m_timer = new PortableTimer(c => OnTick());
            m_ingestClient = ingestClient;
            m_formatProvider = options.FormatProvider;
            m_databaseName = options.DatabaseName;
            m_tableName = options.TableName;
            m_ingestionMapping = ingestionMapping;
            m_flushImmediately = options.FlushImmediately;
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
        /// method which checks the minimum logging level
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
        /// method which gets invoked during the end of every time interval 
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

                        // Skip missing files instead of resetting to the first file
                        if (position.File == null || !IOFile.Exists(position.File))
                        {
                            position = new FileSetPosition(0, files.FirstOrDefault(f => IOFile.Exists(f)));
                        }

                        TPayload payload;
                        if (position.File == null)
                        {
                            payload = m_payloadReader.GetNoPayload();
                            count = 0;
                        }
                        else
                        {
                            payload = m_payloadReader.ReadPayload(m_batchPostingLimit, m_eventBodyLimitBytes, ref position, ref count, position.File);
                        }

                        var stopWatch = Stopwatch.StartNew();
                        var fileIdentifier = Guid.NewGuid();

                        if (count > 0 || (m_controlledSwitch.IsActive && m_nextRequiredLevelCheckUtc < DateTime.UtcNow))
                        {
                            for (int retry = 1; retry <= m_options.IngestionRetries; ++retry)
                            {
                                IKustoIngestionResult result;
                                m_nextRequiredLevelCheckUtc = DateTime.UtcNow.Add(RequiredLevelCheckInterval);
                                using var dataStream = CreateStreamFromLogEvents(payload);
                                try
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
                                        bookmarkFile.WriteBookmark(position); // Update bookmark only after successful ingestion
                                        break; // Exit retry loop on success
                                    }
                                    else
                                    {
                                        m_connectionSchedule.MarkFailure();
                                        if (m_bufferSizeLimitBytes.HasValue)
                                            m_fileSet.CleanUpBufferFiles(m_bufferSizeLimitBytes.Value, 2);
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
                                    SelfLog.WriteLine(" sink due to authentication error (EmitBatchAsync). Please check your credentials.", ex);

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
                                catch (Exception ex) when (retry < m_options.IngestionRetries)
                                {
                                    SelfLog.WriteLine($"Retry {retry} failed: {ex.Message}");
                                }
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

        private static bool FileIsUnlockedAndUnextended(FileSetPosition position)
        {
            try
            {
                using (var fileStream = IOFile.Open(position.File, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    return fileStream.Length <= position.NextLineStart;
                }
            }
            catch (IOException)
            {
                // Where no HRESULT is available, assume IOExceptions indicate a locked file
                return false;
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Unexpected exception while testing locked status of {0}: {1}", position.File, ex);
                return false;
            }
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
