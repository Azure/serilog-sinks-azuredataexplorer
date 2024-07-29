export ingestionURI="https://ingest-sdktestcluster.southeastasia.dev.kusto.windows.net/"
export databaseName="e2e"
export tableName="Serilog"

dotnet user-secrets set "Serilog:WriteTo:0:Args:ingestionUri" $ingestionURI
