# Getting started with Serilog and Azure Data Explorer connector sample project
Let's get started with the installation and configuration of the Serilog-ADX connector.

### Installing the Serilog sink for Azure Data Explorer
The first step in ingesting log data into Azure Data Explorer is to install the Serilog sink for Azure Data Explorer. The sink provides a way to send log data from your .NET application to Azure Data Explorer in real-time. To install the sink, you can use the following NuGet package:
```bash
Install-Package Serilog.Sinks.AzureDataExplorer
```

Once the package is installed, you can configure the sink using the following code:
```c#
var log = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.AzureDataExplorerSink(new AzureDataExplorerSinkOptions
                {
                    IngestionEndpointUri = "<ADXIngestionURL>",
                    DatabaseName = "<databaseName>",
                    TableName = "<tableName>",
                    BufferBaseFileName = "<bufferFileName>",
                    ColumnsMapping = "<mappingName>" ,
                    }
                }.WithAadApplicationKey("<appId>", "<appKey>", "<tenant>"))
                .CreateLogger();
```
Replace the placeholders with the appropriate values for your Azure Data Explorer cluster. You can find these values in the Azure portal.
These steps mentioned above can be used to configure Serilog-ADX connector for any .NET application.

Now we will take a look how to setup the sample Serilog ADX application
### Setting up our Serilog ADX Demo Application
Serilog-ADX connector provides a demo/sample application that can be used to quickly get started with producing logs that can be ingested into the ADX cluster.

- [Create Azure Data Explorer Cluster and DB](https://docs.microsoft.com/en-us/azure/data-explorer/create-cluster-database-portal)
- [Create Azure Active Directory App Registration and grant it permissions to DB](https://docs.microsoft.com/en-us/azure/kusto/management/access-control/how-to-provision-aad-app) (
  save the app key and the application ID for later).
- Create a table in Azure Data Explorer which will be used to store log data. For example, we have created a table with the name "Serilogs".
```sql
.create table Serilogs (Timestamp: datetime, Level: string, Message: string, Exception: string, Properties: dynamic, Position: dynamic, Elapsed: int)
```
- Clone the Serilog-ADX connector git repo
- Navigate to the src/Serilog.Sinks.AzureDataExplorer.Samples folder and open Program.cs. Provide values for the following
  - IngestionEndPointUri : Ingest URL of ADX cluster created.
  - DatabaseName : The name of the database to which data should be ingested into.
  - TableName : The name of the table created (in our case Serilog)
  - AppId : Application Client ID required for authentication.
  - AppKey : Application key required for authentication.
  - Tenant : Tenant Id
  - BufferBaseFileName : If we require durability of our logs(ie we don't want to lose our logs incase of any connection failure to ADX cluster), Ex: C:/Users/logs/Serilog
- The above mentioned parameters needs to be set as environment variables in the respective environments. 
 
For Windows, in powershell set the following parameters

```shell
$env:ingestionURI="<ingestionURI>"
$env:databaseName="<databaseName>"
$env:tableName="<tableName>"
$env:appId="<appId>"
$env:appKey="<appKey>"
$env:tenant="<tenant"
```
 For Linux based environments, in terminal set the following parameters
```shell
export ingestionURI="<ingestionURI>"
export databaseName="<databaseName>"
export tableName="<tableName>"
export appId="<appId>"
export appKey="<appKey>"
export tenant="<tenant"
```
- Open a Powershell window, navigate to Serilog-ADX connector base folder and run the following command
```shell
dotnet build src
```
- Navigate to src/Serilog.Sinks.AzureDataExplorer.Samples/ folder and run the following command
```shell
dotnet run
 ```
- The Sample/Program.cs contains predefined logs which will start getting ingested to ADX.
- The ingested log data can be verified by querying the created log table(Serilogs in our case) by using the following KQL command.
```sql
Serilogs | take 10
```

