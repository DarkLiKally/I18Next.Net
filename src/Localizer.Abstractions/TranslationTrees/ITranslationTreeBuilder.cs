namespace Localizer.TranslationTrees;

public interface ITranslationTreeBuilder
{
    string Namespace { get; set; }

    void AddTranslation(string key, string text);

    ITranslationTree Build();
}
