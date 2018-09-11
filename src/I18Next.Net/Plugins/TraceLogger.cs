using System.Diagnostics;

namespace I18Next.Net.Plugins
{
    public class TraceLogger : ILogger
    {
        public void Error(string message, params object[] args)
        {
            Trace.TraceError(message, args);
        }

        public void Information(string message, params object[] args)
        {
            Trace.TraceInformation(message, args);
        }

        public void Warning(string message, params object[] args)
        {
            Trace.TraceWarning(message, args);
        }
    }
}
