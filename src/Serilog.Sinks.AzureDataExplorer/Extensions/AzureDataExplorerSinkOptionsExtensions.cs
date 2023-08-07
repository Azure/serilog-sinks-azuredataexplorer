using Kusto.Cloud.Platform.Utils;
using Kusto.Data;

namespace Serilog.Sinks.AzureDataExplorer.Extensions
{
    internal static class AzureDataExplorerSinkOptionsExtensions
    {
        private const string AppName = "Serilog.Sinks.AzureDataExplorer";
        private const string ClientVersion = "2.0.0";
        private const string IngestPrefix = "ingest-";
        private const string ProtocolSuffix = "://";
        public static KustoConnectionStringBuilder GetIngestKcsb(
            this AzureDataExplorerSinkOptions options)
        {
            // The connection string in most circumstances will not be an ingest endpoint. Just adding a double check on this.
            string dmConnectionStringEndpoint = options.ConnectionString.Contains(IngestPrefix) ? options.ConnectionString : options.ConnectionString.ReplaceFirstOccurrence(ProtocolSuffix, ProtocolSuffix + IngestPrefix);
            // For ingest we need not have all the options
            return GetKcsbWithAuthentication(dmConnectionStringEndpoint.Split("?")[0], options);
        }

        public static KustoConnectionStringBuilder GetEngineKcsb(
            this AzureDataExplorerSinkOptions options)
        {
            string engineConnectionStringEndpoint = options.ConnectionString.Contains(IngestPrefix) ? options.ConnectionString : options.ConnectionString.ReplaceFirstOccurrence(IngestPrefix, "");
            return GetKcsbWithAuthentication(engineConnectionStringEndpoint, options);
        }

        private static KustoConnectionStringBuilder GetKcsbWithAuthentication(string connectionUrl,
            AzureDataExplorerSinkOptions options)
        {
            KustoConnectionStringBuilder.DefaultPreventAccessToLocalSecretsViaKeywords = false;
            var baseKcsb = new KustoConnectionStringBuilder(connectionUrl)
            {
                ClientVersionForTracing = $"{AppName}:{ClientVersion}"
            };
            var kcsb = string.IsNullOrEmpty(options.ManagedIdentity) ? baseKcsb : ("system".Equals(options.ManagedIdentity, StringComparison.OrdinalIgnoreCase) ? baseKcsb.WithAadSystemManagedIdentity() : baseKcsb.WithAadUserManagedIdentity(options.ManagedIdentity));
            kcsb.ApplicationNameForTracing = AppName;
            kcsb.ClientVersionForTracing = ClientVersion;
            kcsb.SetConnectorDetails(AppName, ClientVersion, "Serilog", "2.12.0");
            return kcsb;
        }
    }
}
