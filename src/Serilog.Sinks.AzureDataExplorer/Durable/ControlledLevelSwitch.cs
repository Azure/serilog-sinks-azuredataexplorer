using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.AzureDataExplorer.Durable
{
    /// <summary>
    /// Instances of this type are single-threaded, generally only updated on a background
    /// timer thread. An exception is <see cref="IsIncluded(LogEvent)"/>, which may be called
    /// concurrently but performs no synchronization.
    /// https://github.com/serilog/serilog-sinks-seq/blob/v4.0.0/src/Serilog.Sinks.Seq/Sinks/Seq/ControlledLevelSwitch.cs
    /// The "ControlledLevelSwitch" class is used to control the minimum logging level of a "LoggingLevelSwitch" object.
    /// The class keeps track of the original logging level of the "LoggingLevelSwitch" object and allows it to be updated to a new level specified by the server.
    /// The class provides methods to check if it is active, and to determine if a given log event is included at the current logging level.
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
