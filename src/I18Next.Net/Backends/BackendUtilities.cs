namespace I18Next.Net.Backends
{
    internal static class BackendUtilities
    {
        public static string GetLanguagePart(string language)
        {
            var index = language.IndexOf('-');

            if (index == -1)
                return language;

            return language.Substring(0, index);
        }
    }
}
