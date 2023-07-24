# Serilog.Sinks.AzureDataExplorer

[![.NET](https://github.com/saguiitay/serilog-sinks-azuredataexplorer/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/saguiitay/serilog-sinks-azuredataexplorer/actions/workflows/dotnet.yml) [![Nuget](https://github.com/saguiitay/serilog-sinks-azuredataexplorer/actions/workflows/nuget.yml/badge.svg)](https://github.com/saguiitay/serilog-sinks-azuredataexplorer/actions/workflows/nuget.yml)

A Serilog sink that writes events to an [Azure Data Explorer (Kusto)](https://docs.microsoft.com/en-us/azure/data-explorer) cluster.

**Package** - [Serilog.Sinks.AzureDataExplorer](http://nuget.org/packages/serilog.sinks.azuredataexplorer)
| **Platforms** - .Net 6.0

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
        TableName = "Serilogs"
        BufferBaseFileName = "BufferBaseFileName"
    })
    .CreateLogger();
```

## Features

* Supports both Queued and Streaming ingestion
* Supports [Data Mappings](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/mappings)
* Supports durable mode where the log is written to a file first and then flushed to ADX Database
* Supports AAD user and applications authentication
* Supports [Azure Data Explorer](https://docs.microsoft.com/en-us/azure/data-explorer),
  [Azure Synapse Data Explorer](https://docs.microsoft.com/en-us/azure/synapse-analytics/data-explorer/data-explorer-overview) and
  [Azure Data Explorer Free-Tier](https://docs.microsoft.com/en-us/azure/data-explorer/start-for-free)
  [Real Time Analytics Platform](https://learn.microsoft.com/en-us/fabric/real-time-analytics/overview)

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

### Durable Mode

Durable mode can be turned on when we specify the bufferFileName in the LoggerConfiguration. There are few other options available when the durable mode is enabled.

* BufferBaseFileName : Enables the durable mode. When specified, the logs are written to the bufferFileName first and then ingested to ADX.

* BufferFileRollingInterval : The interval at which buffer log files will roll over to a new file. The default is RollingInterval.Hour

* BufferLogShippingInterval : The interval between checking the buffer files.

* BufferFileSizeLimitBytes : The maximum size, in bytes, to which the buffer log file for a specific date will be allowed to grow. By default 10 MB is applied

* BufferFileLoggingLevelSwitch : A switch allowing the pass-through minimum level to be changed at runtime.

* BufferFileCountLimit : The maximum number of log files that will be retained, including the current log file. For unlimited retention, pass null. The default is 20.

### Authentication

The sink supports authentication using various methods. These are configured through the [connection string](https://github.com/MicrosoftDocs/dataexplorer-docs/blob/main/data-explorer/kusto/api/connection-strings/kusto.md) options provided.

The following provide examples of these options : 

```csharp
new AzureDataExplorerSinkOptions(
    ConnectionString=$"Data Source={kustoUri};Database=NetDefaultDB;Fed=True;AppClientId={appId};AppKey={appKey};Authority Id={authority}"
)
```
To use SystemManagedIdentity:
```csharp
new AzureDataExplorerSinkOptions(
    ConnectionString=$"Data Source={kustoUri};Database=NetDefaultDB;Fed=True;",
    ManagedIdentity="system"
)
```
Set the MI client Id (GUID) for user-assigned managed identity :
```csharp
new AzureDataExplorerSinkOptions(
    ConnectionString=$"Data Source={kustoUri};Database=NetDefaultDB;Fed=True;",
    ManagedIdentity="362b9e3c-b4d4-43aa-9be9-bbf306fb7481"
)   
```