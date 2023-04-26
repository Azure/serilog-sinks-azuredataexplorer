using Serilog.Sinks.AzureDataExplorer.Durable;

namespace Serilog.Sinks.AzureDataExplorer
{
    public class PortableTimerTests
    {
        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenOnTickIsNull()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => new PortableTimer(null));
        }

        [Fact]
        public void Start_ThrowsArgumentOutOfRangeException_WhenIntervalIsNegative()
        {
            // Arrange
            var timer = new PortableTimer(_ => Task.CompletedTask);

            // Act and Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => timer.Start(TimeSpan.FromSeconds(-1)));
        }

        [Fact]
        public void Start_ThrowsObjectDisposedException_WhenTimerIsDisposed()
        {
            // Arrange
            var timer = new PortableTimer(_ => Task.CompletedTask);
            timer.Dispose();

            // Act and Assert
            Assert.Throws<ObjectDisposedException>(() => timer.Start(TimeSpan.FromSeconds(1)));
        }

        [Fact]
        public Task Start_ExecutesOnTickFunction_WhenIntervalElapses()
        {
            // Arrange
            var timerInterval = TimeSpan.FromMilliseconds(50);
            var timerStarted = new ManualResetEvent(false);
            var timerCompleted = new ManualResetEvent(false);

            var timer = new PortableTimer(_ =>
            {
                timerStarted.Set();
                Thread.Sleep(100);
                timerCompleted.Set();
                return Task.CompletedTask;
            });

            // Act
            timer.Start(timerInterval);

            // Assert
            Assert.True(timerStarted.WaitOne(timerInterval + TimeSpan.FromMilliseconds(50)), "OnTick function should have been invoked within the specified interval.");
            // Cleanup
            timer.Dispose();
            return Task.CompletedTask;
        }
    }
}
