
namespace Serilog.Sinks.AzureDataExplorer.Durable
{
    /// <summary>
    /// Based on the BatchedConnectionStatus class from <see cref="Serilog.Sinks.PeriodicBatching.PeriodicBatchingSink"/>.
    /// https://github.com/serilog/serilog-sinks-seq/blob/v4.0.0/src/Serilog.Sinks.Seq/Sinks/Seq/ExponentialBackoffConnectionSchedule.cs
    /// </summary>
    public class ExponentialBackoffConnectionSchedule
    {
        static readonly TimeSpan MinimumBackoffPeriod = TimeSpan.FromSeconds(5);
        internal static readonly TimeSpan MaximumBackoffInterval = TimeSpan.FromMinutes(10);

        readonly TimeSpan m_period;

        internal int m_failuresSinceSuccessfulConnection;

        public ExponentialBackoffConnectionSchedule(TimeSpan period)
        {
            if (period < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(period), "The connection retry period must be a positive timespan");

            m_period = period;
        }

        public void MarkSuccess()
        {
            m_failuresSinceSuccessfulConnection = 0;
        }

        public void MarkFailure()
        {
            ++m_failuresSinceSuccessfulConnection;
        }

        public TimeSpan NextInterval
        {
            get
            {
                // Available, and first failure, just try the batch interval
                if (m_failuresSinceSuccessfulConnection <= 1) return m_period;

                // Second failure, start ramping up the interval - first 2x, then 4x, ...
                var backoffFactor = Math.Pow(2, (m_failuresSinceSuccessfulConnection - 1));

                // If the period is ridiculously short, give it a boost so we get some
                // visible backoff.
                var backoffPeriod = Math.Max(m_period.Ticks, MinimumBackoffPeriod.Ticks);

                // The "ideal" interval
                var backedOff = (long)(backoffPeriod * backoffFactor);

                // Capped to the maximum interval
                var cappedBackoff = Math.Min(MaximumBackoffInterval.Ticks, backedOff);

                // Unless that's shorter than the base interval, in which case we'll just apply the period
                var actual = Math.Max(m_period.Ticks, cappedBackoff);

                return TimeSpan.FromTicks(actual);
            }
        }
    }
}
