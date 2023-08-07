using System.Reflection;
using Kusto.Ingest;
using Serilog.Events;
using Serilog.Sinks.AzureDataExplorer.Sinks;

namespace Serilog.Sinks.AzureDataExplorer
{
    public class AzureDataExplorerSinkTests
    {
        [Fact]
        public void Test_AzureDataExplorerSink_Throws_ArgumentNullException_For_Null_Options_DurableMode()
        {
            Assert.Throws<ArgumentNullException>(() => new AzureDataExplorerDurableSink(null));
        }

        [Fact]
        public void Test_AzureDataExplorerSink_constructor_should_throw_exception_if_options_is_null_nonDurableMode()
        {
            // Arrange, Act, Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new AzureDataExplorerSink(null));
            Assert.Equal("options", ex.ParamName);
        }

        [Fact]
        public void Test_AzureDataExplorerSink_Throws_ArgumentNullException_For_Null_DatabaseName_DurableMode()
        {
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = null,
                TableName = "test",
                ConnectionString = "https://test.com",
                BufferBaseFileName = "test",
                FlushImmediately = true
            };

            Assert.Throws<ArgumentNullException>(() => new AzureDataExplorerDurableSink(options));
        }

        [Fact]
        public void Test_AzureDataExplorerSink_constructor_should_throw_exception_if_database_name_is_null_NonDurableMode()
        {
            // Arrange
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = null,
                TableName = "table",
                ConnectionString = $"Data Source=http://localhost;Database=NetDefaultDB;Fed=True",
            };

            // Act, Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new AzureDataExplorerSink(options));
            Assert.Equal("DatabaseName", ex.ParamName);
        }

        [Fact]
        public void Test_AzureDataExplorerSink_Throws_ArgumentNullException_For_Null_TableName_DurableMode()
        {
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = "test",
                TableName = null,
                ConnectionString = "https://test.com",
                BufferBaseFileName = "test",
                FlushImmediately = true
            };
            Assert.Throws<ArgumentNullException>(() => new AzureDataExplorerDurableSink(options));
        }

        [Fact]
        public void Test_AzureDataExplorerSink_constructor_should_throw_exception_if_table_name_is_null_non_durable_mode()
        {
            // Arrange
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = "db",
                TableName = null,
                ConnectionString = $"Data Source=http://localhost;Database=NetDefaultDB;Fed=True",
            };

            // Act, Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new AzureDataExplorerSink(options));
            Assert.Equal("TableName", ex.ParamName);
        }

        [Fact]
        public void AzureDataExplorerSink_Throws_ArgumentNullException_For_Null_ConnectionString_DurableMode()
        {
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = "test",
                TableName = "test",
                ConnectionString = null,
                BufferBaseFileName = "test",
                FlushImmediately = true
            };

            Assert.Throws<ArgumentNullException>(() => new AzureDataExplorerDurableSink(options));
        }

        [Fact]
        public void Test_AzureDataExplorerSink_constructor_should_throw_exception_if_ingestion_endpoint_uri_is_null_non_durable_mode()
        {
            // Arrange
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = "db",
                TableName = "table",
                ConnectionString = null,
            };

            // Act, Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new AzureDataExplorerSink(options));
            Assert.Equal("ConnectionString", ex.ParamName);
        }

        [Fact]
        public void Test_AzureDataExplorerSink_constructor_should_set_columns_mapping_when_mapping_name_and_columns_mapping_is_not_set_nonDurableMode()
        {
            // Arrange
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = "db",
                TableName = "table",
                ConnectionString = $"Data Source=http://localhost;Database=NetDefaultDB;Fed=True",
            };

            // Act
            var sink = new AzureDataExplorerSink(options);

            // Assert
            Assert.NotNull(sink);
            var fieldInfoIngestionMapping = sink.GetType()
                .GetField("m_ingestionMapping", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(fieldInfoIngestionMapping);
        }

        [Fact]
        public void Test_AzureDataExplorerSink_constructor_should_use_streaming_ingestion_when_flag_is_set_nonDurableMode()
        {
            // Arrange
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = "db",
                TableName = "table",
                ConnectionString = $"Data Source=http://localhost;Database=db;Fed=True",
                MappingName = "mymapping",
                UseStreamingIngestion = true
            };
            // Act
            var sink = new AzureDataExplorerSink(options);

            // Assert
            Assert.NotNull(sink);
            var ingestClientFieldName = sink.GetType()
                .GetField("m_ingestClient", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(ingestClientFieldName);
            Assert.NotNull(ingestClientFieldName.GetValue(sink));
            Assert.IsAssignableFrom<IKustoIngestClient>(ingestClientFieldName.GetValue(sink));
        }

        [Fact]
        public void Test_AzureDataExplorerSink_constructor_should_use_batch_ingestion_when_flag_is_set_NonDurableMode()
        {
            // Arrange
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = "db",
                TableName = "table",
                ConnectionString = $"Data Source=http://localhost;Database=NetDefaultDB;Fed=True",
                MappingName = "mymapping",
                UseStreamingIngestion = false
            };
            // Act
            var sink = new AzureDataExplorerSink(options);

            // Assert
            Assert.NotNull(sink);
            var ingestClientFieldName = sink.GetType()
                .GetField("m_ingestClient", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(ingestClientFieldName);
            Assert.NotNull(ingestClientFieldName.GetValue(sink));
            Assert.IsAssignableFrom<IKustoQueuedIngestClient>(ingestClientFieldName.GetValue(sink));
        }

        [Fact]
        public void TestEmitBatchAsync()
        {
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = "db",
                TableName = "table",
                ConnectionString = $"Data Source=http://localhost;Database=NetDefaultDB;Fed=True",
                UseStreamingIngestion = true,
            };
            var sink = new AzureDataExplorerSink(options);

            // Arrange
            var logEvent1 = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Information,
                null,
                MessageTemplate.Empty,
                Enumerable.Empty<LogEventProperty>()
            );
            var batch = new List<LogEvent>
            {
                logEvent1
            };

            Assert.NotNull(sink);

            var ingestClientFieldName = sink.GetType()
                .GetField("m_ingestClient", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(ingestClientFieldName);
            Assert.NotNull(ingestClientFieldName.GetValue(sink));
            var ingestClient = ingestClientFieldName.GetValue(sink);
            Assert.NotNull(ingestClient);
            var task = sink.EmitBatchAsync(batch);
            Assert.NotNull(task);
        }

        [Fact]
        public void AzureDataExplorerDurableSink_Throws_ArgumentException_For_Empty_BufferBaseFileName_DurableMode()
        {
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = "test",
                TableName = "test",
                ConnectionString = "https://test.com",
                BufferBaseFileName = "",
                FlushImmediately = true
            };

            Assert.Throws<ArgumentException>(() => new AzureDataExplorerDurableSink(options));
        }
    }
}
