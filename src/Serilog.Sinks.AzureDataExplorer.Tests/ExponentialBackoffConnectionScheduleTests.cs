
using Serilog.Sinks.AzureDataExplorer.Durable;

namespace Serilog.Sinks.AzureDataExplorer.Tests
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
