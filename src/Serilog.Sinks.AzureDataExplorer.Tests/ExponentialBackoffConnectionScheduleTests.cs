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

using Serilog.Sinks.AzureDataExplorer.Durable;

namespace Serilog.Sinks.AzureDataExplorer
{
    public class ExponentialBackoffConnectionScheduleTests
    {
        [Fact]
        public void Constructor_ThrowsArgumentOutOfRangeException_WhenPeriodIsNegative()
        {
            // Act and Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExponentialBackoffConnectionSchedule(TimeSpan.FromSeconds(-1)));
        }

        [Fact]
        public void MarkSuccess_ResetsFailuresSinceSuccessfulConnection()
        {
            // Arrange
            var schedule = new ExponentialBackoffConnectionSchedule(TimeSpan.FromSeconds(1));
            schedule.MarkFailure();
            schedule.MarkFailure();
            schedule.MarkFailure();

            // Act
            schedule.MarkSuccess();

            // Assert
            Assert.Equal(0, schedule.m_failuresSinceSuccessfulConnection);
        }

        [Fact]
        public void MarkFailure_IncrementsFailuresSinceSuccessfulConnection()
        {
            // Arrange
            var schedule = new ExponentialBackoffConnectionSchedule(TimeSpan.FromSeconds(1));

            // Act
            schedule.MarkFailure();
            schedule.MarkFailure();
            schedule.MarkFailure();

            // Assert
            Assert.Equal(3, schedule.m_failuresSinceSuccessfulConnection);
        }

        [Fact]
        public void NextInterval_ReturnsPeriod_WhenFailuresSinceSuccessfulConnectionIsZero()
        {
            // Arrange
            var schedule = new ExponentialBackoffConnectionSchedule(TimeSpan.FromSeconds(1));

            // Act
            var result = schedule.NextInterval;

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(1), result);
        }

        [Fact]
        public void NextInterval_ReturnsPeriod_WhenFailuresSinceSuccessfulConnectionIsOne()
        {
            // Arrange
            var schedule = new ExponentialBackoffConnectionSchedule(TimeSpan.FromSeconds(2));
            schedule.MarkFailure();

            // Act
            var result = schedule.NextInterval;

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(2), result);
        }

        [Fact]
        public void NextInterval_ReturnsMaximumInterval_WhenFailuresSinceSuccessfulConnectionIsHigh()
        {
            // Arrange
            var schedule = new ExponentialBackoffConnectionSchedule(TimeSpan.FromSeconds(1));
            for (int i = 0; i < 20; i++)
            {
                schedule.MarkFailure();
            }

            // Act
            var result = schedule.NextInterval;

            // Assert
            Assert.Equal(ExponentialBackoffConnectionSchedule.MaximumBackoffInterval, result);
        }
    }
}
