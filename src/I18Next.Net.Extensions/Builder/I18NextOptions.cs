namespace I18Next.Net.Extensions.Builder
{
    public class I18NextOptions
    {
        public string DefaultLanguage { get; set; } = "en-US";

        public string DefaultNamespace { get; set; } = "translation";

        public bool DetectLanguageOnEachTranslation { get; set; }
    }
}
