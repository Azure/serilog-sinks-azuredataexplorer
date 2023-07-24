using System.Security.Cryptography.X509Certificates;
using Serilog.Core;

namespace Serilog.Sinks.AzureDataExplorer
{
    /// <summary>
    /// class which contains attributes required to configure the sink
    /// </summary>
    public class AzureDataExplorerSinkOptions
    {
        private int m_queueSizeLimit;

        ///<summary>
        /// The maximum number of events to post in a single batch. Defaults to 1000.
        /// </summary>
        public int BatchPostingLimit { get; set; }

        ///<summary>
        /// The time to wait between checking for event batches. Defaults to 10 seconds.
        /// </summary>
        public TimeSpan Period { get; set; }

        /// <summary>
        /// The maximum number of events that will be held in-memory while waiting to ship them to
        /// AzureDataExplorer. Beyond this limit, events will be dropped. The default is 100,000.
        /// </summary>
        public int QueueSizeLimit
        {
            get { return m_queueSizeLimit; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(QueueSizeLimit), "Queue size limit must be non-zero.");
                m_queueSizeLimit = value;
            }
        }

        /// <summary>
        /// Azure Data Explorer endpoint (Ingestion endpoint for Queued Ingestion, Query endpoint for Streaming Ingestion)
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// The name of the database to which data should be ingested to
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// The name of the table to which data should be ingested to
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// The name of the (pre-created) data mapping to use for the ingested data
        /// </summary>
        public string MappingName { get; set; }

        /// <summary>
        /// The explicit columns mapping to use for the ingested data
        /// </summary>
        public IEnumerable<SinkColumnMapping> ColumnsMapping { get; set; }

        /// <summary>
        /// format provider to format log output
        /// </summary>
        public IFormatProvider FormatProvider { get; set; }

        /// <summary>
        /// Whether to use streaming ingestion (reduced latency, at the cost of reduced throughput) or queued ingestion (increased latency, but much higher throughput).
        /// </summary>
        public bool UseStreamingIngestion { get; set; }

        /// <summary>
        /// Enables the durable mode. when specified, the logs are written to the bufferFileName first and then ingested to ADX
        /// </summary>
        public string BufferBaseFileName { get; set; }

        /// <summary>
        /// specifies the output format for produced logs to be written to buffer file
        /// </summary>
        public string BufferFileOutputFormat { get; set; }

        /// <summary>
        /// The interval at which buffer log files will roll over to a new file. The default is <see cref="RollingInterval.Hour"/>.
        /// </summary>
        public RollingInterval BufferFileRollingInterval { get; set; }

        /// <summary>
        /// The interval between checking the buffer files.
        /// </summary>
        public TimeSpan? BufferLogShippingInterval { get; set; }

        ///<summary>
        /// The maximum length of a an event record to be sent. Defaults to: null (No Limit) only used in file buffer mode
        /// </summary>
        public long? SingleEventSizePostingLimit { get; set; }

        /// <summary>
        /// The maximum size, in bytes, to which the buffer log file for a specific date will be allowed to grow. By default 100L * 1024 * 1024 will be applied.
        /// </summary>
        public long? BufferFileSizeLimitBytes { get; set; }

        /// <summary>
        /// A switch allowing the pass-through minimum level to be changed at runtime. 
        /// </summary>
        public LoggingLevelSwitch BufferFileLoggingLevelSwitch { get; set; }

        /// <summary>
        /// The maximum number of log files that will be retained,
        /// including the current log file. For unlimited retention, pass null. The default is 31.
        /// </summary>
        public int? BufferFileCountLimit { get; set; }

        /// <summary>
        /// This property determines whether it is needed to flush the data immediately to ADX cluster,
        /// The default is false.
        /// </summary>
        public bool FlushImmediately { get; set; }

        /// <summary>
        /// determines the authentication mode
        /// </summary>
        public AuthenticationMode AuthenticationMode { get; private set; }

        /// <summary>
        /// application clientId
        /// </summary>
        public string ApplicationClientId { get; private set; }

        public AzureDataExplorerSinkOptions()
        {
            Period = TimeSpan.FromSeconds(10);
            BatchPostingLimit = 1000;
            QueueSizeLimit = 100000;
            BufferFileRollingInterval = RollingInterval.Hour;
            BufferFileCountLimit = 20;
            BufferFileSizeLimitBytes = 10L * 1024 * 1024;
            BufferFileOutputFormat =
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
            FlushImmediately = false;
            // The default uses this, so that other modes are not attempted
            AuthenticationMode = AuthenticationMode.KustoConnectionString;
        }

        #region Authentication builder methods
        public AzureDataExplorerSinkOptions WithAadSystemAssignedManagedIdentity()
        {
            AuthenticationMode = AuthenticationMode.AadSystemManagedIdentity;
            return this;
        }

        public AzureDataExplorerSinkOptions WithAadUserAssignedManagedIdentity(string applicationClientId)
        {
            AuthenticationMode = AuthenticationMode.AadUserManagedIdentity;
            ApplicationClientId = applicationClientId;
            return this;
        }
        #endregion
    }

    public enum AuthenticationMode
    {
        AadUserManagedIdentity,
        AadSystemManagedIdentity,
        KustoConnectionString
    }
}
