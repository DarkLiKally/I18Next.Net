using Microsoft.Extensions.Logging;
using ILogger = I18Next.Net.Plugins.ILogger;

namespace I18Next.Net.Extensions
{
    public class DefaultExtensionsLogger : ILogger
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public DefaultExtensionsLogger(Microsoft.Extensions.Logging.ILogger logger)
        {
            _logger = logger;
        }

        public void Error(string message, params object[] args)
        {
            _logger.LogError(message, args);
        }

        public void Information(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }

        public void Warning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }
    }
}
