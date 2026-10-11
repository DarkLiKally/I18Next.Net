using System;
using System.Threading.Tasks;

using Godot;

using I18Next.Net.Backends;
using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Godot;

/// <summary>
///     Loads JSON translation files from the Godot file system in the <c>{lng}/{ns}.json</c> layout, e.g.
///     <c>res://locales/en/translation.json</c>. Paths like <c>res://</c> also work in exported projects on all platforms.
///     When a regional language like <c>de-AT</c> has no file, the file of the language part <c>de</c> is loaded.
/// </summary>
public class GodotFileBackend : ITranslationBackend
{
    private readonly FuncBackend _backend;

    /// <summary>
    ///     Creates a backend loading the files below the given directory and building the translation trees with the
    ///     given factory.
    /// </summary>
    /// <param name="basePath">
    ///     The Godot path of the directory containing the language directories, e.g. <c>res://locales</c>.
    /// </param>
    /// <param name="treeBuilderFactory">
    ///     The factory creating the builders of the loaded translation trees or <c>null</c> to build hierarchical trees.
    /// </param>
    /// <exception cref="ArgumentNullException">If <paramref name="basePath" /> is <c>null</c>.</exception>
    public GodotFileBackend(string basePath, ITranslationTreeBuilderFactory treeBuilderFactory)
    {
        if (basePath == null)
            throw new ArgumentNullException(nameof(basePath));

        BasePath = basePath.EndsWith("/", StringComparison.Ordinal) ? basePath : basePath + "/";
        _backend = FuncBackend.FromJson((language, ns) => ReadFile($"{BasePath}{language}/{ns}.json"), treeBuilderFactory);
    }

    /// <summary>
    ///     Creates a backend loading the files below the given directory.
    /// </summary>
    /// <param name="basePath">
    ///     The Godot path of the directory containing the language directories, e.g. <c>res://locales</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">If <paramref name="basePath" /> is <c>null</c>.</exception>
    public GodotFileBackend(string basePath)
        : this(basePath, new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>())
    {
    }

    /// <summary>
    ///     Creates a backend loading the files below <c>res://locales</c> and building the translation trees with the given
    ///     factory.
    /// </summary>
    /// <param name="treeBuilderFactory">
    ///     The factory creating the builders of the loaded translation trees or <c>null</c> to build hierarchical trees.
    /// </param>
    public GodotFileBackend(ITranslationTreeBuilderFactory treeBuilderFactory)
        : this("res://locales", treeBuilderFactory)
    {
    }

    /// <summary>
    ///     Creates a backend loading the files below <c>res://locales</c>.
    /// </summary>
    public GodotFileBackend()
        : this("res://locales")
    {
    }

    /// <summary>
    ///     The Godot path of the directory containing the language directories, always ending with a slash.
    /// </summary>
    protected string BasePath { get; }

    /// <summary>
    ///     Loads the file <c>{BasePath}{language}/{namespace}.json</c>, or the file of the language part if it does not
    ///     exist.
    /// </summary>
    /// <param name="language">The language of the namespace.</param>
    /// <param name="namespace">The namespace.</param>
    /// <returns>The translations or <c>null</c> if neither file exists.</returns>
    public Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
    {
        return _backend.LoadNamespaceAsync(language, @namespace);
    }

    /// <summary>
    ///     Reads the content of a translation file. Returns <c>null</c> if the file does not exist.
    /// </summary>
    /// <param name="path">The Godot path of the file, e.g. <c>res://locales/en/translation.json</c>.</param>
    /// <returns>The content of the file or <c>null</c> if it does not exist.</returns>
    protected virtual string ReadFile(string path)
    {
        return FileAccess.FileExists(path) ? FileAccess.GetFileAsString(path) : null;
    }
}
