namespace I18Next.Net.Plugins
{
    public interface IPluralResolver
    {
        string GetPluralSuffix(string language, int count);

        bool NeedsPlural(string language);
    }
}
