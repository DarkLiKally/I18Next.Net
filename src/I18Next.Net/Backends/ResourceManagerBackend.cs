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

    public ResourceManagerBackend(ResourceManager resourceManager, string @namespace = "translation")
        : this(new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>())
    {
        Add(@namespace, resourceManager);
    }

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
    public ResourceManagerBackend Add(string @namespace, ResourceManager resourceManager)
    {
        if (string.IsNullOrEmpty(@namespace))
            throw new ArgumentException("Namespace cannot be null or empty.", nameof(@namespace));

        _resourceManagers[@namespace] = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));

        return this;
    }

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
