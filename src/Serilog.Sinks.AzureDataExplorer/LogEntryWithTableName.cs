using Serilog.Events;

namespace Serilog.Sinks.AzureDataExplorer
{
    internal class LogEntryWithTableName
    {
        public string TableName { get; set; }

        public LogEvent Log { get; set; }
    }

}
