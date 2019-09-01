namespace I18Next.Net.TranslationTrees
{
    public class GenericTranslationTreeBuilderFactory<T> : ITranslationTreeBuilderFactory
        where T : ITranslationTreeBuilder, new()
    {
        public ITranslationTreeBuilder Create()
        {
            return new T();
        }
    }
}
