// Copyright 2014 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Serilog.Sinks.AzureDataExplorer.Durable
{
    /// <summary>
    /// Based on the BatchedConnectionStatus class from <see cref="Serilog.Sinks.PeriodicBatching.PeriodicBatchingSink"/>.
    /// this class implements an exponential backoff algorithm for retrying connections.
    /// The class has a period and a counter for the number of failures since the last successful connection.
    /// It also provides a mechanism for marking successes and failures, and calculates the next interval to wait based on the number of failures, subject to a minimum and maximum interval.
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
