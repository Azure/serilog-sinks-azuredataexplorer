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
