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

            using var logger = new LoggerConfiguration()
                .WriteTo.AzureDataExplorerSink(options)
                .CreateLogger();

            Assert.NotNull(logger);
        }
        finally
        {
            Directory.Delete(bufferDir, recursive: true);
        }
    }

    [Fact]
    public void AzureDataExplorerSink_DurableMode_StringOverload_Does_Not_Throw_InvalidCast()
    {
        var bufferDir = Path.Combine(Path.GetTempPath(), "adxsink-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(bufferDir);
        try
        {
            using var logger = new LoggerConfiguration()
                .WriteTo.AzureDataExplorerSink(
                    ingestionUri: "http://ingestionUri",
                    databaseName: "mockDB",
                    tableName: "mockTable",
                    applicationClientId: "clientId",
                    applicationSecret: "secret",
                    tenantId: "tenantId",
                    bufferBaseFileName: Path.Combine(bufferDir, "buffer"))
                .CreateLogger();

            Assert.NotNull(logger);
        }
        finally
        {
            Directory.Delete(bufferDir, recursive: true);
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

    [Theory]
    [InlineData(true, true, null)]
    [InlineData(true, false, "some-token")]
    [InlineData(false, true, "some-token")]
    [InlineData(true, true, "some-token")]
    public void AzureDataExplorerSink_Rejects_Conflicting_Auth_Modes(
        bool isManagedIdentity, bool isWorkloadIdentity, string? userToken)
    {
        var loggerConfiguration = new LoggerConfiguration();

        Assert.Throws<ArgumentException>(() =>
            loggerConfiguration.WriteTo.AzureDataExplorerSink(
                ingestionUri: "http://ingestionUri",
                databaseName: "mockDB",
                tableName: "mockTable",
                applicationClientId: "clientId",
                userToken: userToken,
                isManagedIdentity: isManagedIdentity,
                isWorkloadIdentity: isWorkloadIdentity));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void AzureDataExplorerSink_ManagedIdentity_Rejects_Whitespace_ApplicationClientId(string applicationClientId)
    {
        var loggerConfiguration = new LoggerConfiguration();

        Assert.Throws<ArgumentNullException>(() =>
            loggerConfiguration.WriteTo.AzureDataExplorerSink(
                ingestionUri: "http://ingestionUri",
                databaseName: "mockDB",
                tableName: "mockTable",
                applicationClientId: applicationClientId,
                isManagedIdentity: true));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void AzureDataExplorerSink_Whitespace_UserToken_Treated_As_No_Token(string userToken)
    {
        var loggerConfiguration = new LoggerConfiguration();

        Assert.Throws<ArgumentNullException>(() =>
            loggerConfiguration.WriteTo.AzureDataExplorerSink(
                ingestionUri: "http://ingestionUri",
                databaseName: "mockDB",
                tableName: "mockTable",
                userToken: userToken));
    }

    [Theory]
    [InlineData("ingestionUri", "", "mockDB", "mockTable")]
    [InlineData("ingestionUri", " ", "mockDB", "mockTable")]
    [InlineData("databaseName", "http://ingestionUri", "", "mockTable")]
    [InlineData("databaseName", "http://ingestionUri", " ", "mockTable")]
    [InlineData("tableName", "http://ingestionUri", "mockDB", "")]
    [InlineData("tableName", "http://ingestionUri", "mockDB", " ")]
    public void AzureDataExplorerSink_Rejects_Whitespace_Required_Strings(
        string expectedParam, string ingestionUri, string databaseName, string tableName)
    {
        var loggerConfiguration = new LoggerConfiguration();

        var ex = Assert.Throws<ArgumentNullException>(() =>
            loggerConfiguration.WriteTo.AzureDataExplorerSink(
                ingestionUri: ingestionUri,
                databaseName: databaseName,
                tableName: tableName,
                applicationClientId: "clientId",
                applicationSecret: "secret",
                tenantId: "tenantId"));

        Assert.Equal(expectedParam, ex.ParamName);
    }

    [Theory]
    [InlineData("applicationClientId", "", "secret", "tenantId")]
    [InlineData("applicationClientId", " ", "secret", "tenantId")]
    [InlineData("applicationSecret", "clientId", "", "tenantId")]
    [InlineData("applicationSecret", "clientId", " ", "tenantId")]
    [InlineData("tenantId", "clientId", "secret", "")]
    [InlineData("tenantId", "clientId", "secret", " ")]
    public void AzureDataExplorerSink_AadAppKey_Rejects_Whitespace_Credentials(
        string expectedParam, string applicationClientId, string applicationSecret, string tenantId)
    {
        var loggerConfiguration = new LoggerConfiguration();

        var ex = Assert.Throws<ArgumentNullException>(() =>
            loggerConfiguration.WriteTo.AzureDataExplorerSink(
                ingestionUri: "http://ingestionUri",
                databaseName: "mockDB",
                tableName: "mockTable",
                applicationClientId: applicationClientId,
                applicationSecret: applicationSecret,
                tenantId: tenantId));

        Assert.Equal(expectedParam, ex.ParamName);
    }
}
