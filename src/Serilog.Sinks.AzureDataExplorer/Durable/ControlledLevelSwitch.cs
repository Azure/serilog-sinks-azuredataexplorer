using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.AzureDataExplorer.Durable
{
    /// <summary>
    /// Instances of this type are single-threaded, generally only updated on a background
    /// timer thread. An exception is <see cref="IsIncluded(LogEvent)"/>, which may be called
    /// concurrently but performs no synchronization.
    /// https://github.com/serilog/serilog-sinks-seq/blob/v4.0.0/src/Serilog.Sinks.Seq/Sinks/Seq/ControlledLevelSwitch.cs
    /// </summary>
    class ControlledLevelSwitch
    {
        // If non-null, then background level checks will be performed; set either through the constructor
        // or in response to a level specification from the server. Never set to null after being made non-null.
        LoggingLevelSwitch m_controlledSwitch;
        internal LogEventLevel? OriginalLevel;

        public ControlledLevelSwitch(LoggingLevelSwitch controlledSwitch = null)
        {
            m_controlledSwitch = controlledSwitch;
        }

        public bool IsActive => m_controlledSwitch != null;

        public bool IsIncluded(LogEvent evt)
        {
            // Concurrent, but not synchronized.
            var controlledSwitch = m_controlledSwitch;
            return controlledSwitch == null ||
                (int)controlledSwitch.MinimumLevel <= (int)evt.Level;
        }

        public void Update(LogEventLevel? minimumAcceptedLevel)
        {
            if (minimumAcceptedLevel == null)
            {
                if (m_controlledSwitch != null && OriginalLevel.HasValue)
                    m_controlledSwitch.MinimumLevel = OriginalLevel.Value;

                return;
            }

            if (m_controlledSwitch == null)
            {
                // The server is controlling the logging level, but not the overall logger. Hence, if the server
                // stops controlling the level, the switch should become transparent.
                OriginalLevel = LevelAlias.Minimum;
                m_controlledSwitch = new LoggingLevelSwitch(minimumAcceptedLevel.Value);
                return;
            }

            OriginalLevel ??= m_controlledSwitch.MinimumLevel;

            m_controlledSwitch.MinimumLevel = minimumAcceptedLevel.Value;
        }
    }
}
