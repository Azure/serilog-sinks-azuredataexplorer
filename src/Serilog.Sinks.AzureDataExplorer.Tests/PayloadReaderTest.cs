using Serilog.Sinks.AzureDataExplorer.Durable;

namespace Serilog.Sinks.AzureDataExplorer
{
    public class PayloadReaderTest
    {
        [Fact]
        public void ReadPayload_ReadsFromFile_AtGivenPosition()
        {
            var testFileName = "test.txt";
            var testContent = "line1\nline2\nline3";
            System.IO.File.WriteAllText(testFileName, testContent);
            var fileSetPosition = new FileSetPosition(0, testFileName);
            var count = 0;
            var batchPostingLimit = 3;
            var eventBodyLimitBytes = 10;

            var payloadReader = new TestPayloadReader();
            var payload = payloadReader.ReadPayload(batchPostingLimit, eventBodyLimitBytes, ref fileSetPosition, ref count, testFileName);

            Assert.Equal(3, count);
            Assert.Contains("line1", payload);

            System.IO.File.Delete(testFileName);
        }

        [Fact]
        public void ReadPayload_Handles_EventBodyLimitBytes()
        {
            var testFileName = "test.txt";
            var testContent = "line1\nline2\nline3";
            System.IO.File.WriteAllText(testFileName, testContent);
            var fileSetPosition = new FileSetPosition(0, testFileName);
            var count = 0;
            var batchPostingLimit = 3;
            var eventBodyLimitBytes = 5;

            var payloadReader = new TestPayloadReader();
            var payload = payloadReader.ReadPayload(batchPostingLimit, eventBodyLimitBytes, ref fileSetPosition, ref count, testFileName);

            Assert.Equal(3, count);
            Assert.Contains("line1\n", payload);

            System.IO.File.Delete(testFileName);
        }

        [Fact]
        public void GetNoPayload_Returns_EmptyString()
        {
            var payloadReader = new TestPayloadReader();
            var payload = payloadReader.GetNoPayload();
            Assert.Equal("", payload);
        }

        class TestPayloadReader : APayloadReader<string>
        {
            private string m_payload = "";

            public override string GetNoPayload()
            {
                return "";
            }

            protected override void InitPayLoad(string fileName)
            {
                m_payload = "";
            }

            protected override string FinishPayLoad()
            {
                return m_payload;
            }

            protected override void AddToPayLoad(string nextLine)
            {
                m_payload += nextLine + "\n";
            }
        }
    }
}
