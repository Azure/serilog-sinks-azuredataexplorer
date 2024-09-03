using Azure.Core;
using Azure.Identity;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.AzureDataExplorer;
using Serilog.Sinks.AzureDataExplorer.Extensions;
using Xunit;

public class AppSettingsConfigurationTests
{

    [Fact]
    public void TestAppSettingsConfiguration()
    {
        
        var scopes = new List<string> { Environment.GetEnvironmentVariable("ingestionURI") + "/.default" }.ToArray();
        var tokenRequestContext = new TokenRequestContext(scopes, tenantId: Environment.GetEnvironmentVariable("tenant"));
        var m_accessToken = new AzureCliCredential().GetToken(tokenRequestContext).Token;
        var tableName = AzureDataExplorerSinkE2ETests.m_generatedTableName;
        var e2eTests = new AzureDataExplorerSinkE2ETests();
        AzureDataExplorerSinkE2ETests.CreateKustoTable();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var configurationUpdates = new Dictionary<string, string?>
        {
            { "Serilog:WriteTo:0:Args:ingestionUri", Environment.GetEnvironmentVariable("ingestionURI")},
            { "Serilog:WriteTo:0:Args:databaseName", Environment.GetEnvironmentVariable("databaseName")},
            { "Serilog:WriteTo:0:Args:userToken", m_accessToken },
            { "Serilog:WriteTo:0:Args:tableName", tableName },
        };

        var updatedConfiguration = new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .AddInMemoryCollection(configurationUpdates) 
            .Build();

        var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(updatedConfiguration)
            .WriteTo.Console()
            .CreateLogger();

        Assert.NotNull(logger);

        Log.Logger = logger;

        var position = new { Latitude = 25, Longitude = 134 };
        var elapsedMs = 34;

        Log.Verbose("Processed (AppSettingsJSON) {@Position} in {Elapsed:000} ms.", position, elapsedMs);
        Log.Information("Processed (AppSettingsJSON) {@Position} in {Elapsed:000} ms.", position, elapsedMs);
        Log.Warning("Processed (AppSettingsJSON) {@Position} in {Elapsed:000} ms.", position, elapsedMs);
        Log.Error(new Exception(), "Error occurred while processing (AppSettingsJSON) {@Position} in {Elapsed:000} ms.", position, elapsedMs);
        Log.Debug("Processed (AppSettingsJSON) {@Position} in {Elapsed:000} ms.", position, elapsedMs);

        Log.CloseAndFlush();

        Thread.Sleep(40000);

        var noOfRecordsIngested = e2eTests.GetNoOfRecordsIngestedInAdx("AppSettingsJSON");
        Assert.Equal(5, noOfRecordsIngested);
    }
}
