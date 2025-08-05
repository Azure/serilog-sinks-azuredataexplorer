# Serilog.Sinks.AzureDataExplorer

[![.NET](https://github.com/saguiitay/serilog-sinks-azuredataexplorer/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/saguiitay/serilog-sinks-azuredataexplorer/actions/workflows/dotnet.yml) [![Nuget](https://github.com/saguiitay/serilog-sinks-azuredataexplorer/actions/workflows/nuget.yml/badge.svg)](https://github.com/saguiitay/serilog-sinks-azuredataexplorer/actions/workflows/nuget.yml)

A Serilog sink that writes events to an [Azure Data Explorer (Kusto)](https://docs.microsoft.com/en-us/azure/data-explorer) cluster.

**Package** - [Serilog.Sinks.AzureDataExplorer](http://nuget.org/packages/serilog.sinks.azuredataexplorer)
| **Platforms** - .Net 6.0, .Net 8.0

## Getting started

Install from [NuGet](https://nuget.org/packages/serilog.sinks.azuredataexplorer):

```powershell
Install-Package Serilog.Sinks.AzureDataExplorer
```

## How to use

This sink supports the durable mode where the logs are written to a file first and then flushed to the specified ADX table. This durable mode prevents data loss when the ADX connection couldnt be established. Durable mode can be turned on when we specify the bufferFileName in the LoggerConfiguration as mentioned below.

Configuration when durable mode is not required

```csharp
var log = new LoggerConfiguration()
    .WriteTo.AzureDataExplorer(new AzureDataExplorerSinkOptions
    {
        IngestionEndpointUri = "https://ingest-mycluster.northeurope.kusto.windows.net",
        DatabaseName = "MyDatabase",
        TableName = "Serilogs"
    })
    .CreateLogger();
```

Configuration when durable mode is required

```csharp
var log = new LoggerConfiguration()
    .WriteTo.AzureDataExplorer(new AzureDataExplorerSinkOptions
    {
        IngestionEndpointUri = "https://ingest-mycluster.northeurope.kusto.windows.net",
        DatabaseName = "MyDatabase",
        TableName = "Serilogs",
        BufferBaseFileName = "BufferBaseFileName",
        BufferFileRollingInterval = RollingInterval.Minute
    })
    .CreateLogger();
```
Note: Inorder to get the exception as string mapped to kusto column such as Exception, it is recommended to use ExceptionEx Attribute.
For example:
```csharp
new SinkColumnMapping { ColumnName ="Exception", ColumnType ="string", ValuePath = "$.ExceptionEx" }
```

## Configuration of Azure Data Explorer Serilog sink through appsettings.json

You can configure the Azure Data Explorer Serilog Sink using an appsettings.json file. Below is a sample appsettings.json file that includes the Periodic Batching configuration options: batchPostingLimit, period, and queueSizeLimit.

**Sample appsettings.json contents**
```json
{
  "Serilog": {
    "Using": [ "Serilog.Sinks.AzureDataExplorer" ],
    "MinimumLevel": "Verbose",
    "WriteTo": [
      {
        "Name": "AzureDataExplorerSink",
        "Args": {
          "ingestionUri": "https://ingest-cluster-name",
          "databaseName": "sample",
          "tableName": "table",
          "applicationClientId": "xxxxxxxx",
          "applicationSecret": "xxxxxxx",
          "tenantId": "xxxxxxx",
          "isManagedIdentity": false,
          "isWorkloadIdentity": false,
          "batchPostingLimit": 1000,  // Optional
          "period": 10.0,             // Optional (in seconds)
          "queueSizeLimit": 100000    // Optional
        }
      }
    ]
  }
}
```

## Configuration of Azure Data Explorer Serilog sink through appsettings.xml

You can also configure the Azure Data Explorer Serilog Sink using an `appsettings.xml` file. 

**Sample appsettings.xml contents**
```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <Serilog>
    <Using>Serilog.Sinks.File</Using>
    <Using>Serilog.Sinks.AzureDataExplorer</Using>
    <MinimumLevel Default="Verbose" />
    <WriteTo>
      <Args>
        <Name>AzureDataExplorerSink</Name>
        <Args>
          <ingestionUri>https://ingest-cluster-name</ingestionUri>
          <databaseName>DatabaseName</databaseName>
          <tableName>TableName</tableName>
          <applicationClientId>YOUR_CLIENT_ID</applicationClientId> 
          <applicationSecret>YOUR_APP_SECRET</applicationSecret> 
          <tenantId>YOUR_TENANT_ID</tenantId> 
          <batchPostingLimit>1000</batchPostingLimit> <!-- Optional -->
          <period>10</period> <!-- Optional (in seconds) -->
          <queueSizeLimit>100000</queueSizeLimit> <!-- Optional -->
        </Args>
      </Args>
    </WriteTo>
  </Serilog>
</configuration>
```

**Parameters for Periodic Batching:**

***batchPostingLimit:*** Specifies the maximum number of events to include in a batch. Defaults to 1000.

***period:*** Specifies the time in seconds between checking for event batches to post. Defaults to 10 seconds.

***queueSizeLimit:*** Specifies the maximum number of events in the queue. Once this limit is reached, new events will be dropped until the queue size decreases. Defaults to 100000.

The appsettings.json can be loaded through the following piece of code

```csharp
var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
```

**Required NuGet Packages**

Ensure you have the following NuGet dependencies to enable configuration through appsettings.json:

- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Configuration.Json
- Serilog.Settings.Configuration



## Features

* Supports both Queued and Streaming ingestion
* Supports [Data Mappings](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/mappings)
* Supports durable mode where the log is written to a file first and then flushed to ADX Database
* Supports AAD user and applications authentication
* Supports [Azure Data Explorer](https://docs.microsoft.com/en-us/azure/data-explorer),
  [Azure Synapse Data Explorer](https://docs.microsoft.com/en-us/azure/synapse-analytics/data-explorer/data-explorer-overview) and
  [Azure Data Explorer Free-Tier](https://docs.microsoft.com/en-us/azure/data-explorer/start-for-free)

## Options

### Batching

* BatchPostingLimit: The maximum number of events to post in a single batch. Defaults to 1000.
* Period: The time to wait between checking for event batches. Defaults to 10 seconds.
* QueueSizeLimit: The maximum number of events that will be held in-memory while waiting to ship them to AzureDataExplorer. Beyond this limit, events will be dropped. The default is 100,000.

### Target ADX Cluster

* IngestionEndpointUri: Azure Data Explorer endpoint (Ingestion endpoint for Queued Ingestion, Query endpoint for Streaming Ingestion)
* DatabaseName: The name of the database to which data should be ingested to
* TableName: The name of the table to which data should be ingested to
* FlushImmediately : In case queued ingestion is selected, this property determines if is needed to flush the data immediately to ADX cluster. Not recommended to enable for data with higher workloads. The default is false.

### Mapping

Azure Data Explorer provides data mapping capabilities, allowing the ability to extract data rom the ingested JSONs as part of the ingestion. This allows paying a one-time cost of processing the JSON during ingestion, and reduced cost at query time.

By default, the sink uses the following data mapping:

| Column Name | Column Type | JSON Path    | 
|-------------|-------------|--------------|
| Timestamp   | datetime    | $.Timestamp  |
| Level       | string      | $.Level      |
| Message     | string      | $.Message    |
| Exception   | string      | $.Exception  |
| Properties  | dynamic     | $.Properties |

This mapping can be overridden using the following options:

* MappingName: Use a data mapping configured in ADX.
* ColumnsMapping: Use an ingestion-time data mapping.

  Note: If we need detailed exception messages along with stackTrace, innerExceptionDetails etc please use
| Column Name | Column Type | JSON Path        | 
|-------------|-------------|------------------|
| Exception   | string      | $.ExceptionEx    |

### Durable Mode

Durable mode can be turned on when we specify the bufferFileName in the LoggerConfiguration. There are few other options available when the durable mode is enabled.

* BufferBaseFileName : Enables the durable mode. When specified, the logs are written to the bufferFileName first and then ingested to ADX.

* BufferFileRollingInterval : The interval at which buffer log files will roll over to a new file. The default is RollingInterval.Hour

* BufferLogShippingInterval : The interval between checking the buffer files.

* BufferFileSizeLimitBytes : The maximum size, in bytes, to which the buffer log file for a specific date will be allowed to grow. By default 10 MB is applied

* BufferFileLoggingLevelSwitch : A switch allowing the pass-through minimum level to be changed at runtime.

* BufferFileCountLimit : The maximum number of log files that will be retained, including the current log file. For unlimited retention, pass null. The default is 20.

### Authentication

The sink supports authentication using various methods. Use one of the following methods to configure the desired authentication methods:

```csharp
new AzureDataExplorerSinkOptions()
    .WithXXX(...)
```

| Mode                      | Method                        | Notes                             |
|---------------------------|-------------------------------|-----------------------------------|
| AadUserPrompt             | WithAadUserPrompt             | **Recommended only development!** |
| AadUserToken              | WithAadUserToken              |                                   |
| AadApplicationCertificate | WithAadApplicationCertificate |                                   |
| AadApplicationKey         | WithAadApplicationKey         |                                   |
| AadApplicationSubjectName | WithAadApplicationSubjectName |                                   |
| AadApplicationThumbprint  | WithAadApplicationThumbprint  |                                   |
| AadApplicationToken       | WithAadApplicationToken       |                                   |
| AadAzureTokenCredentials  | WithAadAzureTokenCredentials  |                                   |
| AadUserManagedIdentity    | WithAadUserManagedIdentity    |                                   |
| AadSystemManagedIdentity  | WithAadSystemManagedIdentity  |                                   |
| AadWorkloadIdentity       | WithWorkloadIdentity          |                                   |

Note that if none of the authentication options are provided, AzCliIdentity , followed by AadUserPrompt will be attempted.
