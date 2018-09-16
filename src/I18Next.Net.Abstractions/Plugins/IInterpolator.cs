using System.Collections.Generic;
using System.Threading.Tasks;

namespace I18Next.Net.Plugins
{
    public delegate Task<string> TranslateAsyncDelegate(string language, string key, IDictionary<string, object> args);

    public interface IInterpolator
    {
        bool CanNest(string source);

        Task<string> InterpolateAsync(string source, string key, string language, IDictionary<string, object> args);

        Task<string> NestAsync(string source, string language, IDictionary<string, object> args, TranslateAsyncDelegate translateAsync);
    }
}
