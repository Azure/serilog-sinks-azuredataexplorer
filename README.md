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

## Features

* Supports both Queued and Streaming ingestion
* Supports [Data Mappings](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/mappings)
* Supports AAD user and applications authentication
* Supports [Azure Data Explorer](https://docs.microsoft.com/en-us/azure/data-explorer),
  [Azure Synapse Data Explorer](https://docs.microsoft.com/en-us/azure/synapse-analytics/data-explorer/data-explorer-overview) and
  [Azure Data Explorer Free-Tier](https://docs.microsoft.com/en-us/azure/data-explorer/start-for-free)

## Options

### Batching

* BatchPostingLimit: The maximum number of events to post in a single batch. Defaults to 50.
* Period: The time to wait between checking for event batches. Defaults to 2 seconds.
* QueueSizeLimit: The maximum number of events that will be held in-memory while waiting to ship them to AzureDataExplorer. Beyond this limit, events will be dropped. The default is 100,000.

### Target ADX Cluster

* IngestionEndpointUri: Ingestion endpoint of the target ADX cluster.
* DatabaseName: Database name where the events will be ingested.
* TableName: Table name where the events will be ingested.
* UseStreamingIngestion: Whether to use streaming ingestion (reduced latency, at the cost of reduced throughput) or queued ingestion (increased latency, but much higher throughput).

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
