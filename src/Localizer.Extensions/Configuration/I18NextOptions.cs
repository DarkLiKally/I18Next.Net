using System.Collections.Generic;

namespace Localizer.Extensions.Configuration;

public class I18NextOptions
{
    public string DefaultLanguage { get; set; } = "en-US";

    public string DefaultNamespace { get; set; } = "translation";

    public bool DetectLanguageOnEachTranslation { get; set; }

    public IList<string> FallbackLanguages { get; set; } = new List<string>();
}
