# Setting Up Periodic Batching on Serilog Azure Data Explorer Sink

This guide provides instructions to configure periodic batching for the Serilog Azure Data Explorer sink. Periodic batching allows logs to be sent in batches at regular intervals, improving performance and reducing the number of requests to Azure Data Explorer.

The core implementation is already in place. The users just need to configure a few parameters to enable and customize periodic batching.

## Configuration Parameters

**BufferBaseFileName:** Enables the durable mode. when specified, the logs are written to the bufferFileName first and then ingested to ADX
'BufferBaseFileName' is not null here.

**BatchPostingLimit:** The maximum number of events to post in a single batch. Defaults to 1000.

**Period:** The time to wait between checking for event batches. Defaults to 10 seconds.

**QueueSizeLimit:** The maximum number of events that will be held in-memory while waiting to ship them to AzureDataExplorer. Beyond this limit, events will be dropped. The default is 100,000.

## Sample Project
A sample project is included here to provide a reference implementation. The sample demonstrates the usage of periodic batching with the Azure Kusto sink. 

### Quick Start 
For Getting started with Serilog and Azure Data Explorer, and detailed steps on project set up, check out the sample available on src/Serilog.Sinks.AzureDataExplorer.Samples 

To enable Periodic Batching configure the sink options as shown below. 

```
var log = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.AzureDataExplorerSink(new AzureDataExplorerSinkOptions
                {
                    IngestionEndpointUri = "ingestionURI",
                    DatabaseName = "databaseName",
                    TableName = "tableName",
                    FlushImmediately = "flushImmediately".IsNotNullOrEmpty() && bool.Parse("flushImmediately")!,
                    //configure the following variables to enable Periodic Batching
                    BufferBaseFileName = "bufferBaseFileName",
                    BatchPostingLimit = 10, 
                    Period = TimeSpan.FromSeconds(5),

                    ColumnsMapping = new[]
                    {...},
                    }
                }.WithAadUserToken(m_accessToken)).CreateLogger();
```
