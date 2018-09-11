using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Internal
{
    internal class GenericTranslationTreeBuilderFactory<T> : ITranslationTreeBuilderFactory
        where T : ITranslationTreeBuilder, new()
    {
        public ITranslationTreeBuilder Create()
        {
            return new T();
        }
    }
}
