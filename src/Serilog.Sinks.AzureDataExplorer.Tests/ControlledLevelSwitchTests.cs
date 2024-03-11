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

using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.AzureDataExplorer.Durable;

namespace Serilog.Sinks.AzureDataExplorer
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    public class ControlledLevelSwitchTests
    {
        [Fact]
        public void IsActive_ReturnsFalse_WhenConstructedWithoutLoggingLevelSwitch()
        {
            // Arrange
            var controlledLevelSwitch = new ControlledLevelSwitch();

            // Act
            var result = controlledLevelSwitch.IsActive;

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsActive_ReturnsTrue_WhenConstructedWithLoggingLevelSwitch()
        {
            // Arrange
            var loggingLevelSwitch = new LoggingLevelSwitch();
            var controlledLevelSwitch = new ControlledLevelSwitch(loggingLevelSwitch);

            // Act
            var result = controlledLevelSwitch.IsActive;

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(LogEventLevel.Debug, LogEventLevel.Debug, true)]
        [InlineData(LogEventLevel.Debug, LogEventLevel.Information, true)]
        [InlineData(LogEventLevel.Information, LogEventLevel.Debug, false)]
        public void IsIncluded_ReturnsExpectedResult_WhenCalledWithLogEvent(LogEventLevel loggingLevelSwitchMinimum, LogEventLevel logEventLevel, bool expectedResult)
        {
            // Arrange
            var loggingLevelSwitch = new LoggingLevelSwitch(loggingLevelSwitchMinimum);
            var controlledLevelSwitch = new ControlledLevelSwitch(loggingLevelSwitch);
            var logEvent = new LogEvent(DateTimeOffset.UtcNow, logEventLevel, null, MessageTemplate.Empty, Array.Empty<LogEventProperty>());

            // Act
            var result = controlledLevelSwitch.IsIncluded(logEvent);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void Update_SetsMinimumAcceptedLevel_WhenCalledWithNonNullValue()
        {
            // Arrange
            var loggingLevelSwitch = new LoggingLevelSwitch();
            var controlledLevelSwitch = new ControlledLevelSwitch(loggingLevelSwitch);

            // Act
            controlledLevelSwitch.Update(LogEventLevel.Warning);

            // Assert
            Assert.Equal(LogEventLevel.Warning, loggingLevelSwitch.MinimumLevel);
        }

        [Fact]
        public void Update_ResetsMinimumAcceptedLevel_WhenCalledWithNullValue()
        {
            // Arrange
            var loggingLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Warning);
            var controlledLevelSwitch = new ControlledLevelSwitch(loggingLevelSwitch);
            controlledLevelSwitch.Update(LogEventLevel.Information);

            // Act
            controlledLevelSwitch.Update(null);

            // Assert
            Assert.Equal(LogEventLevel.Warning, loggingLevelSwitch.MinimumLevel);
        }

        [Fact]
        public void Update_DoesNotChangeOriginalLevel_WhenCalledWithNullValue()
        {
            // Arrange
            var loggingLevelSwitch = new LoggingLevelSwitch();
            var controlledLevelSwitch = new ControlledLevelSwitch(loggingLevelSwitch);
            // Act
            controlledLevelSwitch.Update(LogEventLevel.Warning);
            controlledLevelSwitch.Update(null);

            // Assert
            Assert.Equal(controlledLevelSwitch.OriginalLevel, LogEventLevel.Information);
        }
    }
}
