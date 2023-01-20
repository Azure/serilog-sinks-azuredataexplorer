using System.Globalization;
using Serilog.Events;
using Serilog.Formatting.Compact.Reader;
using Serilog.Sinks.AzureDataExplorer.Extensions;

namespace Serilog.Sinks.AzureDataExplorer.Durable
{
    /// <summary>
    /// 
    /// </summary>
    public class AzureDataExplorerPayloadReader: APayloadReader<List<LogEvent>>
    {
        private readonly RollingInterval m_rollingInterval;
        private List<LogEvent> m_payload;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rollingInterval"></param>
        public AzureDataExplorerPayloadReader( RollingInterval rollingInterval)
        {
            if ((int)rollingInterval < (int)RollingInterval.Day)
            {
                throw new ArgumentException("Rolling intervals less frequent than RollingInterval.Day are not supported");
            }
            m_rollingInterval = rollingInterval;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override List<LogEvent> GetNoPayload()
        {
            return new List<LogEvent>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        protected override void InitPayLoad(string filename)
        {
            m_payload = new List<LogEvent>();
            var lastToken = filename.Split('-').Last();

            // lastToken should be something like 20150218.log or 20150218_3.log now
            if (!lastToken.ToLowerInvariant().EndsWith(".clef"))
            {
                throw new FormatException(string.Format("The file name '{0}' does not seem to follow the right file pattern - it must be named [whatever]-{{Date}}[_n].clef", Path.GetFileName(filename)));
            }

            var dateFormat = m_rollingInterval.GetFormat();
            var dateString = lastToken.Substring(0, dateFormat.Length);
            DateTime.ParseExact(dateString, dateFormat, CultureInfo.InvariantCulture);
        }
       /// <summary>
       /// 
       /// </summary>
       /// <returns></returns>
        protected override List<LogEvent> FinishPayLoad()
        {
            return m_payload;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nextLine"></param>
        protected override void AddToPayLoad(string nextLine)
        {
            var evt = LogEventReader.ReadFromString(nextLine);
            m_payload.Add(evt);
        }
    }
}
