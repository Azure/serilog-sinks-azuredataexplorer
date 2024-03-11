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

using Serilog.Sinks.AzureDataExplorer.Extensions;

namespace Serilog.Sinks.AzureDataExplorer
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
