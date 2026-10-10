using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace I18Next.Net.Generators;

internal sealed class ResourceCatalog
{
    private readonly Lazy<Dictionary<string, HashSet<string>>> _candidates;
    private readonly List<ResourceFile> _files;
    private readonly Dictionary<string, HashSet<string>> _keys = [];
    private readonly Lazy<Dictionary<string, Dictionary<string, List<ResourceEntry>>>> _variants;

    private ResourceCatalog(ResourceTarget target, List<ResourceFile> files)
    {
        Target = target;
        _files = files;

        foreach (var file in GetSourceFiles())
        {
            if (!_keys.TryGetValue(file.Namespace, out var keys))
                _keys[file.Namespace] = keys = [];

            keys.UnionWith(ResourceModel.GetResolvableKeys(file.Entries, target.JsonFormatVersion));
        }

        _candidates = new Lazy<Dictionary<string, HashSet<string>>>(CreateCandidates);
        _variants = new Lazy<Dictionary<string, Dictionary<string, List<ResourceEntry>>>>(CreateVariants);
    }

    public IEnumerable<string> Namespaces => _keys.Keys;

    public ResourceTarget Target { get; }

    public static ResourceCatalog Create(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken)
    {
        var attributeType = compilation.GetTypeByMetadataName(ResourceTarget.AttributeName);

        if (attributeType == null)
            return null;

        var targets = FindTargets(compilation.Assembly.GlobalNamespace, attributeType).ToList();

        if (targets.Count == 0)
            return null;

        var files = options.AdditionalFiles
            .Select(f => ResourceFile.Create(f, cancellationToken))
            .Where(f => f != null && f.Error == null && targets.Any(t => f.IsIn(t.Path)))
            .OrderBy(f => f.Path, StringComparer.Ordinal)
            .ToList();

        var catalog = new ResourceCatalog(targets[0], files);

        return catalog._keys.Count == 0 ? null : catalog;
    }

    public bool TryGetKeys(string @namespace, out HashSet<string> keys)
    {
        return _keys.TryGetValue(@namespace, out keys);
    }

    /// <summary>
    ///     Returns the keys of a namespace which are worth suggesting: keys without plural suffixes and groups.
    /// </summary>
    public HashSet<string> GetCandidates(string @namespace)
    {
        return _candidates.Value.TryGetValue(@namespace, out var candidates) ? candidates : [];
    }

    public ResourceFile GetSourceFile(string @namespace)
    {
        return GetSourceFiles().FirstOrDefault(f => f.Namespace == @namespace);
    }

    /// <summary>
    ///     Returns the files of the other languages next to the source file of a namespace.
    /// </summary>
    public IEnumerable<ResourceFile> GetTranslationFiles(ResourceFile sourceFile)
    {
        return _files.Where(f => f != sourceFile && f.Namespace == sourceFile.Namespace && f.Directory == sourceFile.Directory);
    }

    /// <summary>
    ///     Returns the entries of the source language which are the key itself or its context and plural variants.
    /// </summary>
    public IReadOnlyList<ResourceEntry> GetSourceVariants(string @namespace, string key)
    {
        var sourceFile = GetSourceFile(@namespace);

        return sourceFile != null && _variants.Value[sourceFile.Path].TryGetValue(key, out var variants) ? variants : [];
    }

    /// <summary>
    ///     Returns the entries of every language which are the key itself or its context and plural variants.
    /// </summary>
    public IEnumerable<ResourceEntry> GetAllVariants(string @namespace, string key)
    {
        return _files
            .Where(f => f.Namespace == @namespace)
            .SelectMany(f => _variants.Value[f.Path].TryGetValue(key, out var variants) ? variants : []);
    }

    private IEnumerable<ResourceFile> GetSourceFiles()
    {
        return _files.Where(f => f.Language == Target.SourceLanguage);
    }

    private Dictionary<string, HashSet<string>> CreateCandidates()
    {
        var candidates = new Dictionary<string, HashSet<string>>();

        foreach (var file in GetSourceFiles())
        {
            if (!candidates.TryGetValue(file.Namespace, out var keys))
                candidates[file.Namespace] = keys = [];

            foreach (var key in ResourceModel.GetKeyIndex(file.Entries, Target.JsonFormatVersion).Keys)
            {
                keys.Add(key);

                for (var i = key.IndexOf('.'); i > 0; i = key.IndexOf('.', i + 1))
                    keys.Add(key.Substring(0, i));
            }
        }

        return candidates;
    }

    private Dictionary<string, Dictionary<string, List<ResourceEntry>>> CreateVariants()
    {
        var variants = new Dictionary<string, Dictionary<string, List<ResourceEntry>>>();

        foreach (var file in _files)
        {
            var fileVariants = new Dictionary<string, List<ResourceEntry>>();

            foreach (var entry in file.Entries)
            {
                var key = entry.Key;
                var dot = key.LastIndexOf('.');

                Add(fileVariants, key, entry);

                for (var underscore = key.LastIndexOf('_'); underscore > dot + 1; underscore = key.LastIndexOf('_', underscore - 1))
                    Add(fileVariants, key.Substring(0, underscore), entry);
            }

            variants[file.Path] = fileVariants;
        }

        return variants;
    }

    private static void Add(Dictionary<string, List<ResourceEntry>> variants, string key, ResourceEntry entry)
    {
        if (!variants.TryGetValue(key, out var entries))
            variants[key] = entries = [];

        entries.Add(entry);
    }

    private static IEnumerable<ResourceTarget> FindTargets(INamespaceOrTypeSymbol container, INamedTypeSymbol attributeType)
    {
        foreach (var member in container.GetMembers())
        {
            if (member is INamespaceOrTypeSymbol child)
            {
                foreach (var target in FindTargets(child, attributeType))
                    yield return target;
            }

            if (member is not INamedTypeSymbol type)
                continue;

            foreach (var attribute in type.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
                    yield return ResourceTarget.Create(type, attribute, Location.None);
            }
        }
    }
}
