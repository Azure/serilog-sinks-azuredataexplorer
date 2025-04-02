using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.AzureDataExplorer;

public class AppSettingsConfigurationTests
{
    private readonly string? _ingestionUri;
    private readonly string? _databaseName;
    private readonly string? _tableName;
    private readonly string _accessToken;
    private readonly AzureDataExplorerSinkE2ETests _e2eTests;

    public AppSettingsConfigurationTests()
    {
        _ingestionUri = Environment.GetEnvironmentVariable("ingestionURI") ?? throw new ArgumentNullException(nameof(_ingestionUri), "Environment variable 'ingestionURI' is required.");
        _databaseName = Environment.GetEnvironmentVariable("databaseName") ?? throw new ArgumentNullException(nameof(_databaseName), "Environment variable 'databaseName' is required.");
        _tableName = AzureDataExplorerSinkE2ETests.m_generatedTableName ?? throw new ArgumentNullException(nameof(_tableName), "Table name is required.");

        var scopes = new[] { $"{_ingestionUri}/.default" };
        var tokenRequestContext = new TokenRequestContext(scopes, tenantId: Environment.GetEnvironmentVariable("tenant"));
        _accessToken = new AzureCliCredential().GetToken(tokenRequestContext).Token;

        _e2eTests = new AzureDataExplorerSinkE2ETests();
        AzureDataExplorerSinkE2ETests.CreateKustoTable();
    }

    [Fact]
    public void TestJsonAppSettingsConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var configurationUpdates = new Dictionary<string, string?>
        {
            { "Serilog:WriteTo:0:Args:ingestionUri", _ingestionUri},
            { "Serilog:WriteTo:0:Args:databaseName",  _databaseName},
            { "Serilog:WriteTo:0:Args:userToken", _accessToken },
            { "Serilog:WriteTo:0:Args:tableName", _tableName },
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

        var noOfRecordsIngested = _e2eTests.GetNoOfRecordsIngestedInAdx("AppSettingsJSON");
        Assert.Equal(5, noOfRecordsIngested);
    }
    
    [Fact]
    public void TestXmlSettingsConfiguration()
    {
     
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddXmlFile(path: "appsettings.xml", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var configurationUpdates = new Dictionary<string, string?>
        {
            { "Serilog:WriteTo:Args:Args:ingestionUri", _ingestionUri },
            { "Serilog:WriteTo:Args:Args:databaseName", _databaseName },
            { "Serilog:WriteTo:Args:Args:userToken", _accessToken },
            { "Serilog:WriteTo:Args:Args:tableName", _tableName },
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

        Log.Verbose("Processed (AppSettingsXML) {@Position} in {Elapsed:000} ms.", position, elapsedMs);
        Log.Information("Processed (AppSettingsXML) {@Position} in {Elapsed:000} ms.", position, elapsedMs);
        Log.Warning("Processed (AppSettingsXML) {@Position} in {Elapsed:000} ms.", position, elapsedMs);
        Log.Error(new Exception(), "Error occurred while processing (AppSettingsXML) {@Position} in {Elapsed:000} ms.", position, elapsedMs);
        Log.Debug("Processed (AppSettingsXML) {@Position} in {Elapsed:000} ms.", position, elapsedMs);

        Log.CloseAndFlush();

        Thread.Sleep(40000);

        var noOfRecordsIngested = _e2eTests.GetNoOfRecordsIngestedInAdx("AppSettingsXML");
        Assert.Equal(5, noOfRecordsIngested);
    }
}