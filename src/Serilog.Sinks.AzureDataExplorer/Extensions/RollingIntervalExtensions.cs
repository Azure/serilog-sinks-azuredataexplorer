namespace Serilog.Sinks.AzureDataExplorer.Extensions
{
    internal static class RollingIntervalExtensions
    {
        // From https://github.com/serilog/serilog-sinks-file/blob/dev/src/Serilog.Sinks.File/Sinks/File/RollingIntervalExtensions.cs#L19
        public static string GetFormat(this RollingInterval interval)
        {
            switch (interval)
            {
                case RollingInterval.Infinite:
                    return "";
                case RollingInterval.Year:
                    return "yyyy";
                case RollingInterval.Month:
                    return "yyyyMM";
                case RollingInterval.Day:
                    return "yyyyMMdd";
                case RollingInterval.Hour:
                    return "yyyyMMddHH";
                case RollingInterval.Minute:
                    return "yyyyMMddHHmm";
                default:
                    throw new ArgumentException("Invalid rolling interval");
            }
        }

        public static string GetMatchingDateRegularExpressionPart(this RollingInterval interval)
        {
            switch (interval)
            {
                case RollingInterval.Infinite:
                    return "";
                case RollingInterval.Year:
                    return "\\d{4}";
                case RollingInterval.Month:
                    return "\\d{6}";
                case RollingInterval.Day:
                    return "\\d{8}";
                case RollingInterval.Hour:
                    return "\\d{10}";
                case RollingInterval.Minute:
                    return "\\d{12}";
                default:
                    throw new ArgumentException("Invalid rolling interval");
            }
        }
    }
}
