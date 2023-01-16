using Kusto.Data;

namespace Serilog.Sinks.AzureDataExplorer
{
    internal static class AzureDataExplorerSinkOptionsExtensions
    {
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
            ;
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
                case AuthenticationMode.AadManagedIdentity:
                    kcsb = kcsb.WithAadUserManagedIdentity(options.ApplicationClientId);
                    break;
                case AuthenticationMode.AadUserPrompt:
                default:
                    kcsb = kcsb.WithAadUserPromptAuthentication(options.Authority);
                    break;
            }

            kcsb.ApplicationNameForTracing = "Serilog.Sink.AzureDataExplorer";
            kcsb.ClientVersionForTracing = typeof(AzureDataExplorerSinkOptionsExtensions).Assembly.GetName().Version?.ToString();
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