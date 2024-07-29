# Setting Up Periodic Batching on Serilog Azure Data Explorer Sink

This guide provides instructions to configure periodic batching for the Serilog Azure Data Explorer sink. Periodic batching allows logs to be sent in batches at regular intervals, improving performance and reducing the number of requests to Azure Data Explorer. Periodic batching can be enabled for durable mode as well as non durable mode.

The core implementation is already in place. The users just need to configure a few parameters to enable and customize periodic batching.

## Configuration Parameters

**BufferBaseFileName:** Enables the durable mode. When specified, the logs are written to the bufferFileName first and then ingested to ADX
'BufferBaseFileName' is not null here.

**BatchPostingLimit:** The maximum number of events to post in a single batch. Defaults to 1000.

**Period:** The time to wait between checking for event batches. Defaults to 10 seconds.

**QueueSizeLimit:** The maximum number of events that will be held in-memory while waiting to ship them to AzureDataExplorer. Beyond this limit, events will be dropped. The default is 100,000.

## Sample Project
A sample project is included here to provide a reference implementation. The sample demonstrates the usage of periodic batching with the Azure Kusto sink. 

### Quick Start 
For Getting started with Serilog and Azure Data Explorer, and detailed steps on project set up, check out the sample available on src/Serilog.Sinks.AzureDataExplorer.Samples 

To enable Periodic Batching configure the sink options as shown below. 
This sink supports the durable mode where the logs are written to a file first and then flushed to the specified ADX table. This durable mode prevents data loss when the ADX connection couldnt be established. Durable mode can be turned on when we specify the bufferFileName in the LoggerConfiguration as mentioned below.


#### Periodic Batching for Durable Mode: 
Configuring the BufferBaseFileName enables the Durable mode.

```csharp
var log = new LoggerConfiguration()
    .WriteTo.AzureDataExplorer(new AzureDataExplorerSinkOptions
    {
        IngestionEndpointUri = "https://ingest-mycluster.northeurope.kusto.windows.net",
        DatabaseName = "MyDatabase",
        TableName = "Serilogs",
        BufferBaseFileName = "BufferBaseFileName",
        BufferFileRollingInterval = RollingInterval.Minute
        //configure the following variables to enable Periodic Batching
        BatchPostingLimit = 10, 
        Period = TimeSpan.FromSeconds(5),

    })
    .CreateLogger();
```

#### Periodic Batching for Non Durable Mode

```csharp
var log = new LoggerConfiguration()
    .WriteTo.AzureDataExplorer(new AzureDataExplorerSinkOptions
    {
        IngestionEndpointUri = "https://ingest-mycluster.northeurope.kusto.windows.net",
        DatabaseName = "MyDatabase",
        TableName = "Serilogs",
        //configure the following variables to enable Periodic Batching
        BatchPostingLimit = 10, 
        Period = TimeSpan.FromSeconds(5),

    })
    .CreateLogger();
```
