using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using System.Threading.Tasks;

using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends;

/// <summary>
///     Loads translations from compiled resources like the <c>ResourceManager</c> of a .resx designer class, with one
///     resource manager per namespace. Satellite assemblies provide the other languages.
/// </summary>
public class ResourceManagerBackend : ITranslationBackend
{
    private readonly Dictionary<string, ResourceManager> _resourceManagers = new(StringComparer.Ordinal);
    private readonly ITranslationTreeBuilderFactory _treeBuilderFactory;

    /// <summary>
    ///     Creates a backend providing a single namespace from the given resource manager, e.g.
    ///     <c>new ResourceManagerBackend(Strings.ResourceManager)</c>. More namespaces can be added with <see cref="Add" />.
    /// </summary>
    /// <param name="resourceManager">The resource manager providing the translations of the namespace.</param>
    /// <param name="namespace">The namespace. Defaults to <c>translation</c>, the default namespace of i18next.</param>
    /// <exception cref="ArgumentNullException"><paramref name="resourceManager" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="namespace" /> is null or empty.</exception>
    public ResourceManagerBackend(ResourceManager resourceManager, string @namespace = "translation")
        : this(new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>())
    {
        Add(@namespace, resourceManager);
    }

    /// <summary>
    ///     Creates a backend without namespaces using the given tree builders. Add the namespaces with <see cref="Add" />.
    /// </summary>
    /// <param name="treeBuilderFactory">Creates the builders of the loaded translation trees.</param>
    /// <exception cref="ArgumentNullException"><paramref name="treeBuilderFactory" /> is null.</exception>
    public ResourceManagerBackend(ITranslationTreeBuilderFactory treeBuilderFactory)
    {
        _treeBuilderFactory = treeBuilderFactory ?? throw new ArgumentNullException(nameof(treeBuilderFactory));
    }

    /// <summary>
    ///     The language of the neutral resources, which are compiled into the main assembly.
    /// </summary>
    public string NeutralLanguage { get; set; } = "en";

    /// <summary>
    ///     Adds the resources of a namespace.
    /// </summary>
    /// <param name="namespace">The namespace.</param>
    /// <param name="resourceManager">The resource manager providing the translations of the namespace.</param>
    /// <returns>The current backend instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="namespace" /> is null or empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="resourceManager" /> is null.</exception>
    public ResourceManagerBackend Add(string @namespace, ResourceManager resourceManager)
    {
        if (string.IsNullOrEmpty(@namespace))
            throw new ArgumentException("Namespace cannot be null or empty.", nameof(@namespace));

        _resourceManagers[@namespace] = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));

        return this;
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Loads the string resources of the culture of the language, else of its language part, without falling back to
    ///     parent cultures. The <see cref="NeutralLanguage" /> loads the neutral resources. Returns null for unknown
    ///     namespaces, unknown cultures and cultures without resources.
    /// </remarks>
    public Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
    {
        if (!_resourceManagers.TryGetValue(@namespace, out var resourceManager))
            return Task.FromResult<ITranslationTree>(null);

        var resourceSet = GetResourceSet(resourceManager, language) ?? GetResourceSet(resourceManager, BackendUtilities.GetLanguagePart(language));

        if (resourceSet == null)
            return Task.FromResult<ITranslationTree>(null);

        var builder = _treeBuilderFactory.Create();
        builder.Namespace = @namespace;

        foreach (DictionaryEntry entry in resourceSet)
        {
            if (entry.Value is string text)
                builder.AddTranslation((string)entry.Key, text);
        }

        return Task.FromResult(builder.Build());
    }

    private ResourceSet GetResourceSet(ResourceManager resourceManager, string language)
    {
        CultureInfo culture;

        try
        {
            culture = string.Equals(language, NeutralLanguage, StringComparison.OrdinalIgnoreCase)
                ? CultureInfo.InvariantCulture
                : CultureInfo.GetCultureInfo(language);

            return resourceManager.GetResourceSet(culture, true, false);
        }
        catch (Exception ex) when (ex is CultureNotFoundException or MissingManifestResourceException)
        {
            return null;
        }
    }
}
