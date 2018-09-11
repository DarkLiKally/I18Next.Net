using System.Threading.Tasks;

namespace I18Next.Net
{
    public interface II18Next
    {
        string Language { get; }

        string T(string key, object args = null);

        Task<string> Ta(string key, object args = null);
    }
}
