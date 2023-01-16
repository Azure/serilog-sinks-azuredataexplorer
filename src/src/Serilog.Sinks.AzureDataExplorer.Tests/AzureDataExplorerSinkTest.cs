using System.Reflection;
using Kusto.Ingest;
using Serilog.Events;
using Serilog.Sinks.AzureDataExplorer.Sinks;

namespace Serilog.Sinks.AzureDataExplorer.Tests
{
    public class AzureDataExplorerSinkTest
    {
        [Fact]
        public void Test_constructor_should_throw_exception_if_options_is_null()
        {
            // Arrange, Act, Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new AzureDataExplorerSink(null));
            Assert.Equal("options", ex.ParamName);
        }

        [Fact]
        public void Test_constructor_should_throw_exception_if_database_name_is_null()
        {
            // Arrange
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = null,
                TableName = "table",
                IngestionEndpointUri = "http://localhost",
            };

            // Act, Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new AzureDataExplorerSink(options));
            Assert.Equal("DatabaseName", ex.ParamName);
        }

        [Fact]
        public void Test_constructor_should_throw_exception_if_table_name_is_null()
        {
            // Arrange
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = "db",
                TableName = null,
                IngestionEndpointUri = "http://localhost",
            };

            // Act, Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new AzureDataExplorerSink(options));
            Assert.Equal("TableName", ex.ParamName);
        }

        [Fact]
        public void Test_constructor_should_throw_exception_if_ingestion_endpoint_uri_is_null()
        {
            // Arrange
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = "db",
                TableName = "table",
                IngestionEndpointUri = null,
            };

            // Act, Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new AzureDataExplorerSink(options));
            Assert.Equal("IngestionEndpointUri", ex.ParamName);
        }

        [Fact]
        public void Test_constructor_should_set_columns_mapping_when_mapping_name_and_columns_mapping_is_not_set()
        {
            // Arrange
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = "db",
                TableName = "table",
                IngestionEndpointUri = "http://localhost",
            };

            // Act
            var sink = new AzureDataExplorerSink(options);

            // Assert
            Assert.NotNull(sink);
            var fieldInfoMappingName =
                sink.GetType().GetField("m_mappingName", BindingFlags.NonPublic | BindingFlags.Instance);
            var fieldInfoIngestionMapping = sink.GetType()
                .GetField("m_ingestionMapping", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(fieldInfoIngestionMapping);
        }

        [Fact]
        public void Test_constructor_should_set_columns_mapping_when_mapping_name_is_set()
        {
            // Arrange
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = "db",
                TableName = "table",
                IngestionEndpointUri = "http://localhost",
                MappingName = "mymapping"
            };

            // Act
            var sink = new AzureDataExplorerSink(options);

            // Assert
            Assert.NotNull(sink);
            var fieldInfoMappingName =
                sink.GetType().GetField("m_mappingName", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(fieldInfoMappingName);
            Assert.Equal(fieldInfoMappingName.GetValue(sink), "mymapping");
        }

        [Fact]
        public void Test_constructor_should_set_log_sink_when_bufferFileName_is_set()
        {
            // Arrange
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = "db",
                TableName = "table",
                IngestionEndpointUri = "http://localhost",
                MappingName = "mymapping",
                BufferFileName = "logTest.txt"
            };
            // Act
            var sink = new AzureDataExplorerSink(options);

            // Assert
            Assert.NotNull(sink);
            var mSinkFieldName = sink.GetType().GetField("m_sink", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(mSinkFieldName);
            Assert.NotNull(mSinkFieldName.GetValue(sink));

            var durableModeFieldName =
                sink.GetType().GetField("m_durableMode", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(durableModeFieldName);
            Assert.NotNull(durableModeFieldName.GetValue(sink));
            Assert.True((bool?)durableModeFieldName.GetValue(sink));
        }

        [Fact]
        public void Test_constructor_should_use_streaming_ingestion_when_flag_is_set()
        {
            // Arrange
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = "db",
                TableName = "table",
                IngestionEndpointUri = "http://localhost",
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
        public void Test_constructor_should_use_batch_ingestion_when_flag_is_set()
        {
            // Arrange
            var options = new AzureDataExplorerSinkOptions
            {
                DatabaseName = "db",
                TableName = "table",
                IngestionEndpointUri = "http://localhost",
                MappingName = "mymapping",
                UseStreamingIngestion = false
            };
            // Act
            var sink = new AzureDataExplorerSink(options);

            // Assert
            Assert.NotNull(sink);
            var ingestClientFieldName = sink.GetType()
                .GetField("m_kustoQueuedIngestClient", BindingFlags.NonPublic | BindingFlags.Instance);
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
                IngestionEndpointUri = "http://localhost",
                UseStreamingIngestion = true,
                BufferFileName = "logTest.txt"
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
            var batch = new List<LogEvent> { logEvent1 };

            Assert.NotNull(sink);
            var mSinkFieldName = sink.GetType().GetField("m_sink", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(mSinkFieldName);
            var m_sink = mSinkFieldName.GetValue(sink);
            Assert.NotNull(m_sink);

            var ingestClientFieldName = sink.GetType()
                .GetField("m_ingestClient", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(ingestClientFieldName);
            Assert.NotNull(ingestClientFieldName.GetValue(sink));
            var ingestClient = ingestClientFieldName.GetValue(sink);
            Assert.NotNull(ingestClient);
            var task = sink.EmitBatchAsync(batch);
            Assert.NotNull(task);
        }
    }
}