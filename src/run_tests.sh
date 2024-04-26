export ingestionURI="https://ingest-sdktestcluster.southeastasia.dev.kusto.windows.net"
export databaseName="e2e"
export tableName=Serilogs
export flushImmediately=true
export bufferBaseFileName=Serilog-temp-buffer
export appId="314cd011-52fd-4093-9c2f-6f6c3c85b60c"
export tenant="72f988bf-86f1-41af-91ab-2d7cd011db47"

dotnet format
dotnet clean
dotnet restore --force-evaluate
dotnet build
dotnet test
