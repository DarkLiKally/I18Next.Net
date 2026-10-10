using I18Next.Net.TranslationTrees;

namespace I18Next.Net.FluentValidation;

internal static class TranslationHelper
{
    public static bool Exists(II18Next i18Next, string language, string key)
    {
        try
        {
            return i18Next.ExistsAsync(language, key).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        catch (TranslationKeyInvalidException)
        {
            return false;
        }
    }

    public static string GetLanguage(II18Next i18Next)
    {
        if (i18Next.DetectLanguageOnEachTranslation && i18Next.LanguageDetector != null)
        {
            var detectedLanguage = i18Next.LanguageDetector.GetLanguage();

            if (!string.IsNullOrWhiteSpace(detectedLanguage))
                return detectedLanguage;
        }

        return i18Next.Language;
    }

    public static string QualifyKey(string @namespace, string key)
    {
        return @namespace == null || key.IndexOf(':') > -1 ? key : @namespace + ":" + key;
    }
}
