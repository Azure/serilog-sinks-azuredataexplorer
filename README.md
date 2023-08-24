# Serilog.Sinks.AzureDataExplorer

A Serilog sink that writes events to an [Azure Data Explorer (Kusto)](https://docs.microsoft.com/en-us/azure/data-explorer) cluster.

**Package** - [Serilog.Sinks.AzureDataExplorer](http://nuget.org/packages/serilog.sinks.azuredataexplorer)
| **Platforms** - .Net 6.0

## Getting started

Install from [NuGet](https://nuget.org/packages/serilog.sinks.azuredataexplorer):

```powershell
Install-Package Serilog.Sinks.AzureDataExplorer
```

## How to use

There are breaking changes between versions 1.0.x and 2.0.0. While there are no change in functionality, the newer version simplifies the configuration options providing connection and authentication options declaratively.

This sink supports the durable mode where the logs are written to a file first and then flushed to the specified ADX table. This durable mode prevents data loss when the ADX connection couldnt be established. Durable mode can be turned on when we specify the bufferFileName in the LoggerConfiguration as mentioned below.

Configuration when durable mode is not required

```csharp
var log = new LoggerConfiguration()
    .WriteTo.AzureDataExplorer(new AzureDataExplorerSinkOptions
    {
        ConnectionString = "Data Source=http://kusto-cluster.region.kusto.windows.net;Database=NetDefaultDB;Fed=True;AppClientId={appId};AppKey={appKey};Authority Id={authority}",
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
        ConnectionString = "Data Source=http://kusto-cluster.region.kusto.windows.net;Database=NetDefaultDB;Fed=True;AppClientId={appId};AppKey={appKey};Authority Id={authority}",
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

## Features

* Supports both Queued and Streaming ingestion
* Supports [Data Mappings](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/mappings)
* Supports durable mode where the log is written to a file first and then flushed to ADX Database
* Supports AAD user and applications authentication
* Supports [Azure Data Explorer](https://docs.microsoft.com/en-us/azure/data-explorer),
  [Azure Synapse Data Explorer](https://docs.microsoft.com/en-us/azure/synapse-analytics/data-explorer/data-explorer-overview) and
  [Azure Data Explorer Free-Tier](https://docs.microsoft.com/en-us/azure/data-explorer/start-for-free)
  [Real time analytics in Fabric](https://learn.microsoft.com/en-us/fabric/real-time-analytics/overview)
* With interactive login, application developers can use [Kusto Free](https://dataexplorer.azure.com/freecluster) to debug and log data from their applications without having to provision a cluster.

## Options

### Batching

* BatchPostingLimit: The maximum number of events to post in a single batch. Defaults to 1000.
* Period: The time to wait between checking for event batches. Defaults to 10 seconds.
* QueueSizeLimit: The maximum number of events that will be held in-memory while waiting to ship them to AzureDataExplorer. Beyond this limit, events will be dropped. The default is 100,000.

### Target ADX Cluster

* IngestionEndpointUri: Azure Data Explorer endpoint (Ingestion endpoint for Queued Ingestion, Query endpoint for Streaming Ingestion)
    - This option is relevant only for package versions **1.0.x** series of the connector
* ConnectionString: The [Kusto connection string](https://learn.microsoft.com/en-us/azure/data-explorer/kusto/api/connection-strings/kusto) to connect to Kusto. This option is relevant only for package versions **2.0.x** series of the connector
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

__**Version 1.0.x**__

The sink supports authentication using various methods. These are configured through the [connection string](https://github.com/MicrosoftDocs/dataexplorer-docs/blob/main/data-explorer/kusto/api/connection-strings/kusto.md) options provided.

This version imperatively declares the options that are needed for Authentication using the __**With**__ methods. The following provide examples of these options : 

```csharp

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

__**Version 2.0.x**__

The sink supports authentication using [Kusto connection string](https://learn.microsoft.com/en-us/azure/data-explorer/kusto/api/connection-strings/kusto) making it easier to declarively configure the authentication options.

For example to use the AppId/AppKey based authentication the user can provide the following connection string

```csharp
$"Data Source={kustoUri};Database=NetDefaultDB;Fed=True;AppClientId={appId};AppKey={appKey};Authority Id={authority}"
```
To use Azure AD Federated authentication using user / application token the user can provide the following connection string

```csharp
$"Data Source={kustoUri};Database=NetDefaultDB;Fed=True;ApplicationToken={appAccessToken}"
```

Using X.509 certificate by thumbprint (client will attempt to load the certificate from local store)

```csharp
$"Data Source={kustoUri};Database=NetDefaultDB;Fed=True;AppClientId={appId};AppCert={appCert};Authority Id={authority}"
```

To use ManagedIdentity, the ManagedIdentity option can be provided that is then used to authenticate the user. Note that the example uses values sourced from environment variables, but can be provided directly as well.

If ManagedIdentity is provided as "system" then the system assigned managed identity is used. If ManagedIdentity is provided as a GUID then the user assigned managed identity is used.

```csharp
var log = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.AzureDataExplorerSink(new AzureDataExplorerSinkOptions
                {
                    ConnectionString = Environment.GetEnvironmentVariable("connectionString"),
                    DatabaseName = Environment.GetEnvironmentVariable("databaseName"),
                    TableName = Environment.GetEnvironmentVariable("tableName"),
                    FlushImmediately = Environment.GetEnvironmentVariable("flushImmediately").IsNotNullOrEmpty() && bool.Parse(Environment.GetEnvironmentVariable("flushImmediately")!),
                    ManagedIdentity = Environment.GetEnvironmentVariable("managedIdentity")
                })

```

To use interactive authentication, the user can provide the following connection string. This is useful for developers to use Kusto Free to debug and log data from their applications without having to provision a cluster.

```csharp
$"Data Source={kustoUri};Database=NetDefaultDB;Fed=True;"
```