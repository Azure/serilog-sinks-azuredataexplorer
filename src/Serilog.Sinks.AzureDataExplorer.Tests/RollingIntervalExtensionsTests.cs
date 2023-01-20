using Serilog.Sinks.AzureDataExplorer.Extensions;

namespace Serilog.Sinks.AzureDataExplorer.Tests
{
    public class RollingIntervalExtensionsTests
    {
        [Theory]
        [InlineData(RollingInterval.Infinite, "")]
        [InlineData(RollingInterval.Year, "yyyy")]
        [InlineData(RollingInterval.Month, "yyyyMM")]
        [InlineData(RollingInterval.Day, "yyyyMMdd")]
        [InlineData(RollingInterval.Hour, "yyyyMMddHH")]
        [InlineData(RollingInterval.Minute, "yyyyMMddHHmm")]
        public void GetFormat_ReturnsExpectedValue(RollingInterval interval, string expected)
        {
            var result = interval.GetFormat();
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(RollingInterval.Infinite, "")]
        [InlineData(RollingInterval.Year, "\\d{4}")]
        [InlineData(RollingInterval.Month, "\\d{6}")]
        [InlineData(RollingInterval.Day, "\\d{8}")]
        [InlineData(RollingInterval.Hour, "\\d{10}")]
        [InlineData(RollingInterval.Minute, "\\d{12}")]
        public void GetMatchingDateRegularExpressionPart_ReturnsExpectedValue(RollingInterval interval, string expected)
        {
            var result = interval.GetMatchingDateRegularExpressionPart();
            Assert.Equal(expected, result);
        }
    }
}
