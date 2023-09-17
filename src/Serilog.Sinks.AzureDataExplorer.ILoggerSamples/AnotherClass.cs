
using Microsoft.Extensions.Logging;

namespace Serilog.Sinks.AzureDataExplorer
{
    internal class AnotherClass
    {
        private readonly ILogger<AnotherClass> m_logger;
        public AnotherClass(ILogger<AnotherClass> logger)
        {
            this.m_logger = logger;
        }

        public void DoSomething()
        {
            this.m_logger.LogInformation("Started function: {functionName}", nameof(DoSomething));
            // do something
            this.m_logger.LogInformation("Ended function: {functionName}", nameof(DoSomething));
        }
    }
}
