namespace I18Next.Net.TranslationTrees;

public interface ITranslationTreeBuilder
{
    string Namespace { get; set; }

    void AddTranslation(string key, string text);

    ITranslationTree Build();
}