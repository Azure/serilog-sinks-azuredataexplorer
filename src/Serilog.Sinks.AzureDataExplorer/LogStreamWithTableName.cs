namespace Serilog.Sinks.AzureDataExplorer
{
    internal class LogStreamWithTableName : IDisposable
    {
        public LogStreamWithTableName(string tableName, Stream stream)
        {
            Stream = stream;
            TableName = tableName;
        }

        public Stream Stream { get; set; }

        public string TableName { get; set; }

        public void Dispose()
        {
            this.Stream?.Dispose();
        }
    }
}
