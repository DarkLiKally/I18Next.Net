using System;
using System.Threading.Tasks;

using Godot;

using I18Next.Net.Backends;
using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Godot;

/// <summary>
///     Loads JSON translation files from the Godot file system in the <c>{lng}/{ns}.json</c> layout, e.g.
///     <c>res://locales/en/translation.json</c>. Paths like <c>res://</c> also work in exported projects on all platforms.
/// </summary>
public class GodotFileBackend : ITranslationBackend
{
    private readonly FuncBackend _backend;

    public GodotFileBackend(string basePath, ITranslationTreeBuilderFactory treeBuilderFactory)
    {
        if (basePath == null)
            throw new ArgumentNullException(nameof(basePath));

        BasePath = basePath.EndsWith("/", StringComparison.Ordinal) ? basePath : basePath + "/";
        _backend = FuncBackend.FromJson((language, ns) => ReadFile($"{BasePath}{language}/{ns}.json"), treeBuilderFactory);
    }

    public GodotFileBackend(string basePath)
        : this(basePath, new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>())
    {
    }

    public GodotFileBackend(ITranslationTreeBuilderFactory treeBuilderFactory)
        : this("res://locales", treeBuilderFactory)
    {
    }

    public GodotFileBackend()
        : this("res://locales")
    {
    }

    protected string BasePath { get; }

    public Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
    {
        return _backend.LoadNamespaceAsync(language, @namespace);
    }

    /// <summary>
    ///     Reads the content of a translation file. Returns <c>null</c> if the file does not exist.
    /// </summary>
    /// <param name="path">The Godot path of the file, e.g. <c>res://locales/en/translation.json</c>.</param>
    protected virtual string ReadFile(string path)
    {
        return FileAccess.FileExists(path) ? FileAccess.GetFileAsString(path) : null;
    }
}
