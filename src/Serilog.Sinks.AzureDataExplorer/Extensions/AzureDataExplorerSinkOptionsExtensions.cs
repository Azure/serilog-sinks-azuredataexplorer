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

using Kusto.Data;

namespace Serilog.Sinks.AzureDataExplorer.Extensions
{
    internal static class AzureDataExplorerSinkOptionsExtensions
    {
        private const string AppName = "Serilog.Sinks.AzureDataExplorer";
        private const string ClientVersion = "1.0.6";
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
