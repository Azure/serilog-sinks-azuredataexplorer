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

using Serilog.Configuration;
using Serilog.Sinks.AzureDataExplorer.Extensions;

namespace Serilog.Sinks.AzureDataExplorer;

public class AzureDataExplorerSinkExtensionsTests
{
    [Fact]
    public void AzureDataExplorerSink_Creates_Valid_Sink_Configuration()
    {
        // Arrange
        var options = new AzureDataExplorerSinkOptions
        {
            DatabaseName = "mockDB",
            TableName = "mockTable",
            IngestionEndpointUri = "http://ingestionUri",
            BatchPostingLimit = 10,
            Period = TimeSpan.FromSeconds(10),
            QueueSizeLimit = 100
        };
        var loggerConfiguration = new LoggerConfiguration();

        // Act
        var sinkConfiguration = loggerConfiguration.WriteTo.AzureDataExplorerSink(options);

        // Assert
        Assert.NotNull(sinkConfiguration);
        Assert.IsType<LoggerConfiguration>(sinkConfiguration);
    }

    [Fact]
    public void AzureDataExplorerSink_Throws_Exception_For_Null_LoggerConfiguration()
    {
        // Arrange
        LoggerSinkConfiguration? loggerConfiguration = null;
        var options = new AzureDataExplorerSinkOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            loggerConfiguration.AzureDataExplorerSink(options));
    }

    [Fact]
    public void AzureDataExplorerSink_Throws_Exception_For_Null_Options()
    {
        // Arrange
        var loggerConfiguration = new LoggerConfiguration();
        AzureDataExplorerSinkOptions? options = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            loggerConfiguration.WriteTo.AzureDataExplorerSink(options));
    }
}
