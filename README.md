# Serilog.Sinks.AzureDataExplorer

[![.NET](https://github.com/saguiitay/serilog-sinks-azuredataexplorer/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/saguiitay/serilog-sinks-azuredataexplorer/actions/workflows/dotnet.yml) [![Nuget](https://github.com/saguiitay/serilog-sinks-azuredataexplorer/actions/workflows/nuget.yml/badge.svg)](https://github.com/saguiitay/serilog-sinks-azuredataexplorer/actions/workflows/nuget.yml)

A Serilog sink that writes events to an [Azure Data Explorer (Kusto)](https://docs.microsoft.com/en-us/azure/data-explorer) cluster.

**Package** - [Serilog.Sinks.AzureDataExplorer](http://nuget.org/packages/serilog.sinks.azuredataexplorer)
| **Platforms** - .Net 6.0

### Getting started

Install from [NuGet](https://nuget.org/packages/serilog.sinks.azuredataexplorer):

```powershell
Install-Package Serilog.Sinks.AzureDataExplorer
```

### How to use

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

### Features

* Queued/Streaming ingestion
* Supports [Azure Data Explorer](https://docs.microsoft.com/en-us/azure/data-explorer),
  [Azure Synapse Data Explorer](https://docs.microsoft.com/en-us/azure/synapse-analytics/data-explorer/data-explorer-overview) and
  [Azure Data Explerere Free-Tier](https://docs.microsoft.com/en-us/azure/data-explorer/start-for-free)
* Supprots [Data Mappings](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/mappings)
* Suppots AAD user and applications authentication

### Options

TODO
