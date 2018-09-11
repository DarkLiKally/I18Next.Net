namespace I18Next.Net.Plugins
{
    public interface ILogger
    {
        void Error(string message, params object[] args);

        void Information(string message, params object[] args);

        void Warning(string message, params object[] args);
    }
}
