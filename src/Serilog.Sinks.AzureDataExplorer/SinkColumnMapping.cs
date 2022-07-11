namespace Serilog.Sinks.Azuredataexplorer
{
    public struct SinkColumnMapping
    {
        public string ColumnName { get; set; }
        public string ColumnType { get; set; }
        public string ValuePath { get; set; }
    }
}