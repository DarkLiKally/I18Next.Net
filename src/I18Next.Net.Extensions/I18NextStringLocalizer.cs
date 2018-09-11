using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Localization;

namespace I18Next.Net.Extensions
{
    public class I18NextStringLocalizer : IStringLocalizer
    {
        private readonly I18Next _instance;

        public I18NextStringLocalizer(I18Next instance)
        {
            _instance = instance;
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            var result = _instance.Backend.LoadNamespaceAsync(_instance.Language, _instance.DefaultNamespace).Result;

            return result.GetAllValues().Select(t => new LocalizedString(t.Key, t.Value));
        }

        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            _instance.Language = culture.TwoLetterISOLanguageName;
            
            return this;
        }

        public LocalizedString this[string name] => new LocalizedString(name, _instance.T(name));

        public LocalizedString this[string name, params object[] arguments] => new LocalizedString(name, _instance.T(name, arguments));
    }
}
