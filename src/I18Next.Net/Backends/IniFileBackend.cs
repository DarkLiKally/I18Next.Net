using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using I18Next.Net.Internal;
using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends
{
    public class IniFileBackend : ITranslationBackend
    {
        private readonly string _basePath;
        private readonly ITranslationTreeBuilderFactory _treeBuilderFactory;

        public IniFileBackend(string basePath)
            : this(basePath, new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>())
        {
        }

        public IniFileBackend(string basePath, ITranslationTreeBuilderFactory treeBuilderFactory)
        {
            _basePath = basePath;
            _treeBuilderFactory = treeBuilderFactory;
        }

        public IniFileBackend(ITranslationTreeBuilderFactory treeBuilderFactory)
            : this("locales", treeBuilderFactory)
        {
        }

        public IniFileBackend()
            : this("locales")
        {
        }

        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public async Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
        {
            var path = FindFile(language, @namespace);

            if (path == null)
                return null;

            string iniContent;

            using (var reader = new StreamReader(path, Encoding))
            {
                iniContent = await reader.ReadToEndAsync();
            }

            var iniReader = new SimpleIniParser(iniContent);

            var builder = _treeBuilderFactory.Create();

            PopulateTreeBuilder(iniReader, builder);

            return builder.Build();
        }

        private string FindFile(string language, string @namespace)
        {
            var path = Path.Combine(_basePath, language, @namespace + ".ini");

            if (File.Exists(path))
                return path;

            path = Path.Combine(_basePath, BackendUtilities.GetLanguagePart(language), @namespace + ".ini");

            return !File.Exists(path) ? null : path;
        }

        private static void PopulateTreeBuilder(SimpleIniParser iniReader, ITranslationTreeBuilder builder)
        {
            foreach (var iniSection in iniReader.GetSections().Concat(new [] { "" }))
            {
                var section = iniSection;

                if (section != string.Empty)
                    section = section + ".";

                foreach (var iniKey in iniReader.GetKeys(iniSection))
                {
                    var key = section + iniKey;
                    var value = iniReader.GetValue(iniSection, iniKey, string.Empty);

                    builder.AddTranslation(key, value);
                }
            }
        }
    }
}
