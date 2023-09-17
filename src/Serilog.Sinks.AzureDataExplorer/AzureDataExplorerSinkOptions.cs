using System.Security.Cryptography.X509Certificates;
using System.Text;
using Serilog.Core;

namespace Serilog.Sinks.AzureDataExplorer
{
    /// <summary>
    /// class which contains attributes required to configure the sink
    /// </summary>
    public class AzureDataExplorerSinkOptions
    {
        private int m_queueSizeLimit;
        private IReadOnlyDictionary<string, string> m_tableNameMappings;

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
        public string IngestionEndpointUri { get; set; }

        /// <summary>
        /// The name of the database to which data should be ingested to
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// The name of the table to which data should be ingested to
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// The source and table name mappings
        /// For the log from different source, it may contains diferent fields and we may have the requirement to ingest them into different tables
        /// For eample: 
        ///     - we may want to ingest ASP.NET related logs to a specific table
        ///     - we may want to ingest performance related logs to a specific table
        ///     - etc.
        /// If the source matches an entry in the mapping table, logs will be directed to the mapping table, otherwise it will directed to the default table <see cref="TableName"/>
        /// Due to performance limitation, source is defined at class level now, not namespace level, each source has to be listed explicitly in the mapping table
        /// </summary>
        public IReadOnlyDictionary<string, string> TableNameMappings
        {
            get { return this.m_tableNameMappings; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(TableNameMappings));
                }

                var copy = new Dictionary<string, string>(value.Count);
                foreach (var entry in value)
                {
                    if (string.IsNullOrWhiteSpace(entry.Key))
                    {
                        throw new ArgumentException("A table name mapping key was null, empty, or consisted only of white-space characters.", nameof(this.TableNameMappings));
                    }

                    if (string.IsNullOrWhiteSpace(entry.Value))
                    {
                        throw new ArgumentException($"The table name mapping value provided for key '{entry.Key}' was null, empty, or consisted only of white-space characters.", nameof(this.TableNameMappings));
                    }

                    if (Encoding.UTF8.GetByteCount(entry.Value) != entry.Value.Length)
                    {
                        throw new ArgumentException($"The table name mapping value '{entry.Value}' provided for key '{entry.Key}' contained non-ASCII characters.", nameof(this.TableNameMappings));
                    }

                    copy[entry.Key] = entry.Value;
                }

                this.m_tableNameMappings = copy;
            }
        }

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
        /// user token
        /// </summary>
        public string UserToken { get; private set; }

        /// <summary>
        /// application token
        /// </summary>
        public string ApplicationToken { get; private set; }

        /// <summary>
        /// application clientId
        /// </summary>
        public string ApplicationClientId { get; private set; }

        /// <summary>
        /// ApplicationCertificateThumbprint
        /// </summary>
        public string ApplicationCertificateThumbprint { get; private set; }

        /// <summary>
        /// ApplicationCertificateSubjectDistinguishedName
        /// </summary>
        public string ApplicationCertificateSubjectDistinguishedName { get; private set; }

        /// <summary>
        /// ApplicationKey
        /// </summary>
        public string ApplicationKey { get; private set; }

        /// <summary>
        /// ApplicationCertificate
        /// </summary>
        public X509Certificate2 ApplicationCertificate { get; private set; }

        /// <summary>
        /// Authority
        /// </summary>
        public string Authority { get; private set; }

        /// <summary>
        /// SendX5C
        /// </summary>
        public bool SendX5C { get; private set; }

        /// <summary>
        /// AzureRegion
        /// </summary>
        public string AzureRegion { get; private set; }

        /// <summary>
        /// TokenCredential
        /// </summary>
        public Azure.Core.TokenCredential TokenCredential { get; private set; }

        public AzureDataExplorerSinkOptions()
        {
            this.Period = TimeSpan.FromSeconds(10);
            this.BatchPostingLimit = 1000;
            this.QueueSizeLimit = 100000;
            this.BufferFileRollingInterval = RollingInterval.Hour;
            this.BufferFileCountLimit = 20;
            this.BufferFileSizeLimitBytes = 10L * 1024 * 1024;
            this.BufferFileOutputFormat =
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
            this.FlushImmediately = false;
        }

        #region Authentication builder methods

        public AzureDataExplorerSinkOptions WithAadApplicationCertificate(string applicationClientId, X509Certificate2 applicationCertificate, string authority,
            bool sendX5C = false, string azureRegion = null)
        {
            AuthenticationMode = AuthenticationMode.AadApplicationCertificate;
            ApplicationClientId = applicationClientId;
            ApplicationCertificate = applicationCertificate;
            Authority = authority;
            SendX5C = sendX5C;
            AzureRegion = azureRegion;

            return this;
        }

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

        public AzureDataExplorerSinkOptions WithAadApplicationKey(string applicationClientId, string applicationKey, string authority)
        {
            AuthenticationMode = AuthenticationMode.AadApplicationKey;
            ApplicationClientId = applicationClientId;
            ApplicationKey = applicationKey;
            Authority = authority;

            return this;
        }

        public AzureDataExplorerSinkOptions WithAadApplicationSubjectName(string applicationClientId, string applicationCertificateSubjectDistinguishedName,
            string authority, string azureRegion = null)
        {
            AuthenticationMode = AuthenticationMode.AadApplicationSubjectName;
            ApplicationClientId = applicationClientId;
            ApplicationCertificateSubjectDistinguishedName = applicationCertificateSubjectDistinguishedName;
            Authority = authority;
            AzureRegion = azureRegion;

            return this;
        }

        public AzureDataExplorerSinkOptions WithAadApplicationThumbprint(string applicationClientId, string applicationCertificateThumbprint, string authority)
        {
            AuthenticationMode = AuthenticationMode.AadApplicationThumbprint;
            ApplicationClientId = applicationClientId;
            ApplicationCertificateThumbprint = applicationCertificateThumbprint;
            Authority = authority;

            return this;
        }

        public AzureDataExplorerSinkOptions WithAadApplicationToken(string applicationToken)
        {
            AuthenticationMode = AuthenticationMode.AadApplicationToken;
            ApplicationToken = applicationToken;

            return this;
        }

        public AzureDataExplorerSinkOptions WithAadApplicationToken(Azure.Core.TokenCredential tokenCredential)
        {
            AuthenticationMode = AuthenticationMode.AadAzureTokenCredentials;
            TokenCredential = tokenCredential;

            return this;
        }

        public AzureDataExplorerSinkOptions WithAadUserToken(string userToken)
        {
            AuthenticationMode = AuthenticationMode.AadUserToken;
            UserToken = userToken;

            return this;
        }

        #endregion
    }

    public enum AuthenticationMode
    {
        AadUserPrompt,
        AadUserToken,
        AadApplicationCertificate,
        AadApplicationKey,
        AadApplicationSubjectName,
        AadApplicationThumbprint,
        AadApplicationToken,
        AadAzureTokenCredentials,
        AadUserManagedIdentity,
        AadSystemManagedIdentity
    }
}
