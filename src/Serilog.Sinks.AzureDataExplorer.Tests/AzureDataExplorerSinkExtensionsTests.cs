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

using Serilog.Configuration;
using Serilog.Sinks.AzureDataExplorer.Extensions;

namespace Serilog.Sinks.AzureDataExplorer;

public class AzureDataExplorerSinkExtensionsTests
{
    [Fact]
    public void AzureDataExplorerSink_Creates_Valid_Sink_Configuration()
    {
        // Arrange
        var options = new AzureDataExplorerSinkOptions
        {
            DatabaseName = "mockDB",
            TableName = "mockTable",
            IngestionEndpointUri = "http://ingestionUri",
            BatchPostingLimit = 10,
            Period = TimeSpan.FromSeconds(10),
            QueueSizeLimit = 100
        };
        var loggerConfiguration = new LoggerConfiguration();

        // Act
        var sinkConfiguration = loggerConfiguration.WriteTo.AzureDataExplorerSink(options);

        // Assert
        Assert.NotNull(sinkConfiguration);
        Assert.IsType<LoggerConfiguration>(sinkConfiguration);
    }

    [Fact]
    public void AzureDataExplorerSink_Throws_Exception_For_Null_LoggerConfiguration()
    {
        // Arrange
        LoggerSinkConfiguration? loggerConfiguration = null;
        var options = new AzureDataExplorerSinkOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            loggerConfiguration.AzureDataExplorerSink(options));
    }

    [Fact]
    public void AzureDataExplorerSink_Throws_Exception_For_Null_Options()
    {
        // Arrange
        var loggerConfiguration = new LoggerConfiguration();
        AzureDataExplorerSinkOptions? options = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            loggerConfiguration.WriteTo.AzureDataExplorerSink(options));
    }

    [Fact]
    public void AzureDataExplorerSink_DurableMode_Options_Overload_Does_Not_Throw_InvalidCast()
    {
        var bufferDir = Path.Combine(Path.GetTempPath(), "adxsink-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(bufferDir);
        try
        {
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = "mockDB",
                TableName = "mockTable",
                IngestionEndpointUri = "http://ingestionUri",
                BufferBaseFileName = Path.Combine(bufferDir, "buffer")
            };
            var loggerConfiguration = new LoggerConfiguration();

            var sinkConfiguration = loggerConfiguration.WriteTo.AzureDataExplorerSink(options);

            Assert.NotNull(sinkConfiguration);
        }
        finally
        {
            try { Directory.Delete(bufferDir, recursive: true); } catch { }
        }
    }

    [Fact]
    public void AzureDataExplorerSink_DurableMode_StringOverload_Does_Not_Throw_InvalidCast()
    {
        var bufferDir = Path.Combine(Path.GetTempPath(), "adxsink-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(bufferDir);
        try
        {
            var loggerConfiguration = new LoggerConfiguration();

            var sinkConfiguration = loggerConfiguration.WriteTo.AzureDataExplorerSink(
                ingestionUri: "http://ingestionUri",
                databaseName: "mockDB",
                tableName: "mockTable",
                applicationClientId: "clientId",
                applicationSecret: "secret",
                tenantId: "tenantId",
                bufferBaseFileName: Path.Combine(bufferDir, "buffer"));

            Assert.NotNull(sinkConfiguration);
        }
        finally
        {
            try { Directory.Delete(bufferDir, recursive: true); } catch { }
        }
    }

    [Fact]
    public void AzureDataExplorerSink_SystemManagedIdentity_Does_Not_Require_Secret_Or_Tenant()
    {
        var loggerConfiguration = new LoggerConfiguration();

        var sinkConfiguration = loggerConfiguration.WriteTo.AzureDataExplorerSink(
            ingestionUri: "http://ingestionUri",
            databaseName: "mockDB",
            tableName: "mockTable",
            applicationClientId: "system",
            isManagedIdentity: true);

        Assert.NotNull(sinkConfiguration);
    }

    [Fact]
    public void AzureDataExplorerSink_UserManagedIdentity_Does_Not_Require_Secret_Or_Tenant()
    {
        var loggerConfiguration = new LoggerConfiguration();

        var sinkConfiguration = loggerConfiguration.WriteTo.AzureDataExplorerSink(
            ingestionUri: "http://ingestionUri",
            databaseName: "mockDB",
            tableName: "mockTable",
            applicationClientId: "11111111-1111-1111-1111-111111111111",
            isManagedIdentity: true);

        Assert.NotNull(sinkConfiguration);
    }

    [Fact]
    public void AzureDataExplorerSink_WorkloadIdentity_Does_Not_Require_Any_Auth_Params()
    {
        var loggerConfiguration = new LoggerConfiguration();

        var sinkConfiguration = loggerConfiguration.WriteTo.AzureDataExplorerSink(
            ingestionUri: "http://ingestionUri",
            databaseName: "mockDB",
            tableName: "mockTable",
            isWorkloadIdentity: true);

        Assert.NotNull(sinkConfiguration);
    }

    [Fact]
    public void AzureDataExplorerSink_UserToken_Does_Not_Require_Secret_Or_Tenant()
    {
        var loggerConfiguration = new LoggerConfiguration();

        var sinkConfiguration = loggerConfiguration.WriteTo.AzureDataExplorerSink(
            ingestionUri: "http://ingestionUri",
            databaseName: "mockDB",
            tableName: "mockTable",
            userToken: "some-token");

        Assert.NotNull(sinkConfiguration);
    }

    [Fact]
    public void AzureDataExplorerSink_AadAppKey_Still_Requires_ApplicationSecret()
    {
        var loggerConfiguration = new LoggerConfiguration();

        Assert.Throws<ArgumentNullException>(() =>
            loggerConfiguration.WriteTo.AzureDataExplorerSink(
                ingestionUri: "http://ingestionUri",
                databaseName: "mockDB",
                tableName: "mockTable",
                applicationClientId: "clientId",
                applicationSecret: null,
                tenantId: "tenantId"));
    }

    [Fact]
    public void AzureDataExplorerSink_AadAppKey_Still_Requires_TenantId()
    {
        var loggerConfiguration = new LoggerConfiguration();

        Assert.Throws<ArgumentNullException>(() =>
            loggerConfiguration.WriteTo.AzureDataExplorerSink(
                ingestionUri: "http://ingestionUri",
                databaseName: "mockDB",
                tableName: "mockTable",
                applicationClientId: "clientId",
                applicationSecret: "secret",
                tenantId: null));
    }

    [Fact]
    public void AzureDataExplorerSink_AadAppKey_Still_Requires_ApplicationClientId()
    {
        var loggerConfiguration = new LoggerConfiguration();

        Assert.Throws<ArgumentNullException>(() =>
            loggerConfiguration.WriteTo.AzureDataExplorerSink(
                ingestionUri: "http://ingestionUri",
                databaseName: "mockDB",
                tableName: "mockTable",
                applicationClientId: null,
                applicationSecret: "secret",
                tenantId: "tenantId"));
    }

    [Fact]
    public void AzureDataExplorerSink_ManagedIdentity_Requires_ApplicationClientId()
    {
        var loggerConfiguration = new LoggerConfiguration();

        Assert.Throws<ArgumentNullException>(() =>
            loggerConfiguration.WriteTo.AzureDataExplorerSink(
                ingestionUri: "http://ingestionUri",
                databaseName: "mockDB",
                tableName: "mockTable",
                applicationClientId: null,
                isManagedIdentity: true));
    }
}
