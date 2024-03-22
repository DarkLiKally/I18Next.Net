using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Localizer.TranslationTrees;

namespace Localizer.Backends;

public class StrictXmlFileBackend : ITranslationBackend
{
    private readonly string _basePath;
    private readonly ITranslationTreeBuilderFactory _treeBuilderFactory;

    public StrictXmlFileBackend(string basePath)
        : this(basePath, new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>())
    {
    }

    public StrictXmlFileBackend(string basePath, ITranslationTreeBuilderFactory treeBuilderFactory)
    {
        _basePath = basePath;
        _treeBuilderFactory = treeBuilderFactory;
    }

    public StrictXmlFileBackend(ITranslationTreeBuilderFactory treeBuilderFactory)
        : this("locales", treeBuilderFactory)
    {
    }

    public StrictXmlFileBackend()
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
            switch (childNode.Name.LocalName)
            {
                case "Section":
                    if (childNode.HasElements)
                    {
                        var sectionAttribute = childNode.Attribute("name");

                        if (sectionAttribute != null)
                        {
                            var sectionKey = path + sectionAttribute.Value;

                            PopulateTreeBuilder(sectionKey, childNode, builder);
                        }
                    }

                    break;
                case "Translation":
                    var attribute = childNode.Attribute("key");

                    if (attribute != null)
                    {
                        var translationKey = path + attribute.Value;

                        builder.AddTranslation(translationKey, childNode.Value);
                    }

                    break;
            }
        }
    }
}