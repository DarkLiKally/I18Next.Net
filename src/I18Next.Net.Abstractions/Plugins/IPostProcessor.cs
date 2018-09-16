using System.Collections.Generic;

namespace I18Next.Net.Plugins
{
    public interface IPostProcessor
    {
        string Keyword { get; }

        string Process(string key, string value, IDictionary<string, object> args);
    }
}
