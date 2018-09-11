using System.Collections.Generic;

namespace I18Next.Net.TranslationTrees
{
    public interface ITranslationTree
    {
        string Namespace { get; set; }

        string GetValue(string key, IDictionary<string, object> args);

        IDictionary<string, string> GetAllValues();
    }
}
