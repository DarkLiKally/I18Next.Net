using System.Collections.Generic;

namespace Localizer.TranslationTrees;

public interface ITranslationTree
{
    string Namespace { get; set; }

    IDictionary<string, string> GetAllValues();

    string GetValue(string key, IDictionary<string, object> args);
}
