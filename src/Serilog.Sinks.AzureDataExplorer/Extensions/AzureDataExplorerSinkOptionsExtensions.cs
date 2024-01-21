using Kusto.Data;

namespace Serilog.Sinks.AzureDataExplorer.Extensions
{
    internal static class AzureDataExplorerSinkOptionsExtensions
    {
        private const string AppName = "Serilog.Sinks.AzureDataExplorer";
        private const string ClientVersion = "1.0.4";
        public static KustoConnectionStringBuilder GetKustoConnectionStringBuilder(
            this AzureDataExplorerSinkOptions options)
        {
            var kcsb = new KustoConnectionStringBuilder(options.IngestionEndpointUri, options.DatabaseName);
            return GetKcsbWithAuthentication(kcsb, options);
        }

        public static KustoConnectionStringBuilder GetKustoEngineConnectionStringBuilder(
            this AzureDataExplorerSinkOptions options)
        {
            var kcsb = new KustoConnectionStringBuilder(GetClusterUrl(options.IngestionEndpointUri),
                options.DatabaseName);
            return GetKcsbWithAuthentication(kcsb, options);
        }

        private static KustoConnectionStringBuilder GetKcsbWithAuthentication(KustoConnectionStringBuilder kcsb,
            AzureDataExplorerSinkOptions options)
        {
            switch (options.AuthenticationMode)
            {
                case AuthenticationMode.AadApplicationCertificate:
                    kcsb = kcsb.WithAadApplicationCertificateAuthentication(options.ApplicationClientId,
                        options.ApplicationCertificate, options.Authority, options.SendX5C, options.AzureRegion);
                    break;
                case AuthenticationMode.AadApplicationKey:
                    kcsb = kcsb.WithAadApplicationKeyAuthentication(options.ApplicationClientId, options.ApplicationKey,
                        options.Authority);
                    break;
                case AuthenticationMode.AadApplicationSubjectName:
                    kcsb = kcsb.WithAadApplicationSubjectNameAuthentication(options.ApplicationClientId,
                        options.ApplicationCertificateSubjectDistinguishedName, options.Authority, options.AzureRegion);
                    break;
                case AuthenticationMode.AadApplicationThumbprint:
                    kcsb = kcsb.WithAadApplicationThumbprintAuthentication(options.ApplicationClientId,
                        options.ApplicationCertificateThumbprint, options.Authority);
                    break;
                case AuthenticationMode.AadApplicationToken:
                    kcsb = kcsb.WithAadApplicationTokenAuthentication(options.ApplicationToken);
                    break;
                case AuthenticationMode.AadAzureTokenCredentials:
                    kcsb = kcsb.WithAadAzureTokenCredentialsAuthentication(options.TokenCredential);
                    break;
                case AuthenticationMode.AadUserToken:
                    kcsb = kcsb.WithAadUserTokenAuthentication(options.UserToken);
                    break;
                case AuthenticationMode.AadSystemManagedIdentity:
                    kcsb = kcsb.WithAadSystemManagedIdentity();
                    break;
                case AuthenticationMode.AadUserManagedIdentity:
                    kcsb = kcsb.WithAadUserManagedIdentity(options.ApplicationClientId);
                    break;
                case AuthenticationMode.AadUserPrompt:
                default:
                    kcsb = kcsb.WithAadUserPromptAuthentication(options.Authority);
                    break;
            }

            kcsb.ApplicationNameForTracing = AppName;
            kcsb.ClientVersionForTracing = ClientVersion;
            kcsb.SetConnectorDetails(AppName, ClientVersion, "Serilog", "2.12.0");
            return kcsb;
        }

        public static string GetClusterUrl(string ingestUrl)
        {
            string[] parts = ingestUrl.Split('-');
            string clusterName = parts.Last();
            return "https://" + clusterName;
        }
    }
}
