using System.Security.Cryptography.X509Certificates;

namespace Serilog.Sinks.Azuredataexplorer
{
    public class AzureDataExplorerSinkOptions
    {
        public const string DefaultDatabaseName = "Diagnostics";
        public const string DefaultTableName = "Logs";
        public const string DefaultMappingName = "SerilogMapping";


        private int m_queueSizeLimit;

        ///<summary>
        /// The maximum number of events to post in a single batch. Defaults to 50.
        /// </summary>
        public int BatchPostingLimit { get; set; }

        ///<summary>
        /// The time to wait between checking for event batches. Defaults to 2 seconds.
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

        public string IngestionEndpointUri { get; set; }
        public string DatabaseName { get; set; }
        public string TableName { get; set; }
        public string MappingName { get; set; }

        public IFormatProvider FormatProvider { get; set; }

        public IEnumerable<SinkColumnMapping> ColumnsMapping { get; set; }

        public bool UseStreamingIngestion { get; set; }

        public AuthenticationMode AuthenticationMode { get; private set; }
        public string UserToken { get; private set; }
        public string ApplicationToken { get; private set; }
        public string ApplicationClientId { get; private set; }
        public string ApplicationCertificateThumbprint { get; private set; }
        public string ApplicationCertificateSubjectDistinguishedName { get; private set; }
        public string ApplicationKey { get; private set; }
        public X509Certificate2 ApplicationCertificate { get; private set; }
        public string Authority { get; private set; }
        public bool SendX5c { get; private set; }
        public string AzureRegion { get; private set; }
        public Azure.Core.TokenCredential TokenCredential { get; private set; }

        public AzureDataExplorerSinkOptions()
        {
            Period = TimeSpan.FromSeconds(2);
            BatchPostingLimit = 50;
            QueueSizeLimit = 25000;

            //ColumnsMapping = new List<SinkColumnMapping>(0);
        }

        public AzureDataExplorerSinkOptions WithAadApplicationCertificate(string applicationClientId, X509Certificate2 applicationCertificate, string authority, bool sendX5c = false, string azureRegion = null)
        {
            AuthenticationMode = AuthenticationMode.AadApplicationCertificate;
            ApplicationClientId = applicationClientId;
            ApplicationCertificate = applicationCertificate;
            Authority = authority;
            SendX5c = sendX5c;
            AzureRegion = azureRegion;

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

        public AzureDataExplorerSinkOptions WithAadApplicationSubjectName(string applicationClientId, string applicationCertificateSubjectDistinguishedName, string authority, string azureRegion = null)
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
        AadAzureTokenCredentials
    }

}