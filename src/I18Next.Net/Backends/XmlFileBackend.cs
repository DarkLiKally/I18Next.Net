using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using I18Next.Net.Internal;
using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends
{
    public class XmlFileBackend : ITranslationBackend
    {
        private readonly string _basePath;
        private readonly ITranslationTreeBuilderFactory _treeBuilderFactory;

        public XmlFileBackend(string basePath)
            : this(basePath, new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>())
        {
        }

        public XmlFileBackend(string basePath, ITranslationTreeBuilderFactory treeBuilderFactory)
        {
            _basePath = basePath;
            _treeBuilderFactory = treeBuilderFactory;
        }

        public XmlFileBackend(ITranslationTreeBuilderFactory treeBuilderFactory)
            : this("locales", treeBuilderFactory)
        {
        }

        public XmlFileBackend()
            : this("locales")
        {
        }

        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
        {
            var path = FindFile(language, @namespace);

            if (path == null)
                return null;

            XContainer parsedXml;

            using (var streamReader = new StreamReader(path, Encoding))
            {
                var document = XDocument.Load(streamReader);

                parsedXml = document.Root;
            }

            var builder = _treeBuilderFactory.Create();

            PopulateTreeBuilder("", parsedXml, builder);

            return Task.FromResult(builder.Build());
        }

        private string FindFile(string language, string @namespace)
        {
            var path = Path.Combine(_basePath, language, @namespace + ".xml");

            if (File.Exists(path))
                return path;

            path = Path.Combine(_basePath, BackendUtilities.GetLanguagePart(language), @namespace + ".xml");

            return !File.Exists(path) ? null : path;
        }

        private static void PopulateTreeBuilder(string path, XContainer node, ITranslationTreeBuilder builder)
        {
            if (path != string.Empty)
                path = path + ".";

            foreach (var childNode in node.Elements())
            {
                var key = path + childNode.Name;

                if (childNode.HasElements)
                    PopulateTreeBuilder(key, childNode, builder);
                else
                    builder.AddTranslation(key, childNode.Value);
            }
        }
    }
}
