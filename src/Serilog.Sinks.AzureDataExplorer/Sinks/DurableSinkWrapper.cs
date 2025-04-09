using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.AzureDataExplorer.Sinks
{
    /// <summary>
    /// A wrapper to adapt AzureDataExplorerDurableSink to work with LoggerSinkConfiguration.
    /// </summary>
    internal class DurableSinkWrapper : ILogEventSink
    {
        private readonly AzureDataExplorerDurableSink _durableSink;

        public DurableSinkWrapper(AzureDataExplorerDurableSink durableSink)
        {
            _durableSink = durableSink ?? throw new ArgumentNullException(nameof(durableSink));
        }

        public void Emit(LogEvent logEvent)
        {
            _durableSink.Emit(logEvent);
        }
    }
}