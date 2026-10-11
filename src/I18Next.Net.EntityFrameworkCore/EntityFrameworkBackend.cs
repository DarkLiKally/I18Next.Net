using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.TranslationTrees;

using Microsoft.EntityFrameworkCore;

namespace I18Next.Net.EntityFrameworkCore;

/// <summary>
///     Loads and stores translations in a database with Entity Framework Core. The contexts are created by an
///     <see cref="IDbContextFactory{TContext}" />, so the backend can be used as a singleton. The model of the context has to
///     contain the <see cref="TranslationEntry" /> entity, see <see cref="ModelBuilderExtensions.ApplyI18NextTranslations" />.
///     Translations changed through the backend are announced with <see cref="TranslationsChanged" />, so translators
///     load them again.
/// </summary>
/// <typeparam name="TContext">The type of the context.</typeparam>
public class EntityFrameworkBackend<TContext> : IWritableTranslationBackend, INotifyingTranslationBackend, IExpiringTranslationBackend
    where TContext : DbContext
{
    private readonly IDbContextFactory<TContext> _contextFactory;
    private readonly ITranslationTreeBuilderFactory _treeBuilderFactory;

    /// <summary>
    ///     Creates a backend which builds hierarchical translation trees, so dotted keys like <c>menu.home</c> are nested.
    /// </summary>
    /// <param name="contextFactory">The factory creating a context for every operation.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="contextFactory" /> is <c>null</c>.</exception>
    public EntityFrameworkBackend(IDbContextFactory<TContext> contextFactory)
        : this(contextFactory, new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>())
    {
    }

    /// <summary>
    ///     Creates a backend which builds the translation trees with the given factory.
    /// </summary>
    /// <param name="contextFactory">The factory creating a context for every operation.</param>
    /// <param name="treeBuilderFactory">The factory creating the builders of the loaded translation trees.</param>
    /// <exception cref="ArgumentNullException">If an argument is <c>null</c>.</exception>
    public EntityFrameworkBackend(IDbContextFactory<TContext> contextFactory, ITranslationTreeBuilderFactory treeBuilderFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _treeBuilderFactory = treeBuilderFactory ?? throw new ArgumentNullException(nameof(treeBuilderFactory));
    }

    /// <summary>
    ///     Raised after translations were changed through <see cref="SaveNamespaceAsync" />, <see cref="SetValueAsync" /> or
    ///     <see cref="RemoveValueAsync" />. Changes made directly in the database are not detected, use
    ///     <see cref="CacheExpiration" /> to pick them up.
    /// </summary>
    public event EventHandler<TranslationsChangedEventArgs> TranslationsChanged;

    /// <summary>
    ///     Whether the language part of a regional language like <c>de</c> for <c>de-DE</c> is loaded when there are no
    ///     translations for the regional language. Defaults to <c>true</c>.
    /// </summary>
    public bool FallbackToLanguagePart { get; set; } = true;

    /// <summary>
    ///     The time after which the translator loads a namespace again, e.g. to pick up translations changed by other servers.
    ///     <c>null</c> keeps loaded namespaces until they are changed through this backend.
    /// </summary>
    public TimeSpan? CacheExpiration { get; set; }

    /// <summary>
    ///     Loads the translations of a namespace. Missing keys without a translation are skipped.
    /// </summary>
    /// <param name="language">The language of the namespace.</param>
    /// <param name="namespace">The namespace.</param>
    /// <returns>
    ///     The translations or <c>null</c> if there are none for the language (and its language part when
    ///     <see cref="FallbackToLanguagePart" /> is enabled).
    /// </returns>
    public async Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
    {
        List<KeyValuePair<string, string>> values;

        using (var context = _contextFactory.CreateDbContext())
        {
            values = await LoadValuesAsync(context, language, @namespace).ConfigureAwait(false);

            if (values.Count == 0 && FallbackToLanguagePart)
            {
                var languagePart = BackendUtilities.GetLanguagePart(language);

                if (languagePart != language)
                    values = await LoadValuesAsync(context, languagePart, @namespace).ConfigureAwait(false);
            }
        }

        if (values.Count == 0)
            return null;

        var builder = _treeBuilderFactory.Create();
        builder.Namespace = @namespace;

        foreach (var value in values)
            builder.AddTranslation(value.Key, value.Value);

        return builder.Build();
    }

    /// <summary>
    ///     Replaces the translations of a namespace with the translations of the given tree, written with a single
    ///     <c>SaveChanges</c>. Missing keys without a translation are kept. Raises <see cref="TranslationsChanged" />.
    /// </summary>
    /// <param name="language">The language of the namespace.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="tree">
    ///     The translations, <see cref="ITranslationTree.GetAllValues" /> provides them with dotted keys.
    /// </param>
    /// <returns>A task which completes when the translations are stored.</returns>
    /// <exception cref="ArgumentException">
    ///     If <paramref name="language" /> or <paramref name="namespace" /> is <c>null</c>, empty or whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">If <paramref name="tree" /> is <c>null</c>.</exception>
    public async Task SaveNamespaceAsync(string language, string @namespace, ITranslationTree tree)
    {
        ValidateArguments(language, @namespace);

        if (tree == null)
            throw new ArgumentNullException(nameof(tree));

        var values = tree.GetAllValues();

        using (var context = _contextFactory.CreateDbContext())
        {
            var entries = await context.Set<TranslationEntry>()
                .Where(e => e.Language == language && e.Namespace == @namespace)
                .ToListAsync()
                .ConfigureAwait(false);
            var existingKeys = new HashSet<string>();

            foreach (var entry in entries)
            {
                existingKeys.Add(entry.Key);

                if (values.TryGetValue(entry.Key, out var value) && value != null)
                    entry.Value = value;
                else if (entry.Value != null)
                    context.Remove(entry);
            }

            foreach (var value in values)
            {
                if (value.Value != null && !existingKeys.Contains(value.Key))
                    context.Add(new TranslationEntry { Language = language, Namespace = @namespace, Key = value.Key, Value = value.Value });
            }

            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        TranslationsChanged?.Invoke(this, new TranslationsChangedEventArgs(language, @namespace));
    }

    /// <summary>
    ///     Adds or updates the translation of a key. Raises <see cref="TranslationsChanged" />.
    /// </summary>
    /// <param name="language">The language of the translation.</param>
    /// <param name="namespace">The namespace of the key.</param>
    /// <param name="key">The key with dots separating nested groups, e.g. <c>menu.home</c>.</param>
    /// <param name="value">The translation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task which completes when the translation is stored.</returns>
    /// <exception cref="ArgumentException">
    ///     If <paramref name="language" />, <paramref name="namespace" /> or <paramref name="key" /> is <c>null</c>,
    ///     empty or whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">If <paramref name="value" /> is <c>null</c>.</exception>
    public async Task SetValueAsync(string language, string @namespace, string key, string value, CancellationToken cancellationToken = default)
    {
        ValidateArguments(language, @namespace);

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null, empty or whitespace string.", nameof(key));
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        using (var context = _contextFactory.CreateDbContext())
        {
            var entry = await FindEntryAsync(context, language, @namespace, key, cancellationToken).ConfigureAwait(false);

            if (entry == null)
                context.Add(new TranslationEntry { Language = language, Namespace = @namespace, Key = key, Value = value });
            else
                entry.Value = value;

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        TranslationsChanged?.Invoke(this, new TranslationsChangedEventArgs(language, @namespace));
    }

    /// <summary>
    ///     Removes the translation of a key. Raises <see cref="TranslationsChanged" /> if the key existed.
    /// </summary>
    /// <param name="language">The language of the translation.</param>
    /// <param name="namespace">The namespace of the key.</param>
    /// <param name="key">The key with dots separating nested groups, e.g. <c>menu.home</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the key existed.</returns>
    /// <exception cref="ArgumentException">
    ///     If <paramref name="language" />, <paramref name="namespace" /> or <paramref name="key" /> is <c>null</c>,
    ///     empty or whitespace.
    /// </exception>
    public async Task<bool> RemoveValueAsync(string language, string @namespace, string key, CancellationToken cancellationToken = default)
    {
        ValidateArguments(language, @namespace);

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null, empty or whitespace string.", nameof(key));

        using (var context = _contextFactory.CreateDbContext())
        {
            var entry = await FindEntryAsync(context, language, @namespace, key, cancellationToken).ConfigureAwait(false);

            if (entry == null)
                return false;

            context.Remove(entry);

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        TranslationsChanged?.Invoke(this, new TranslationsChangedEventArgs(language, @namespace));

        return true;
    }

    private static Task<List<KeyValuePair<string, string>>> LoadValuesAsync(TContext context, string language, string @namespace)
    {
        return context.Set<TranslationEntry>()
            .AsNoTracking()
            .Where(e => e.Language == language && e.Namespace == @namespace && e.Value != null)
            .OrderBy(e => e.Id)
            .Select(e => new KeyValuePair<string, string>(e.Key, e.Value))
            .ToListAsync();
    }

    private static Task<TranslationEntry> FindEntryAsync(TContext context, string language, string @namespace, string key,
        CancellationToken cancellationToken)
    {
        return context.Set<TranslationEntry>()
            .FirstOrDefaultAsync(e => e.Language == language && e.Namespace == @namespace && e.Key == key, cancellationToken);
    }

    private static void ValidateArguments(string language, string @namespace)
    {
        if (string.IsNullOrWhiteSpace(language))
            throw new ArgumentException("Language cannot be null, empty or whitespace string.", nameof(language));
        if (string.IsNullOrWhiteSpace(@namespace))
            throw new ArgumentException("Namespace cannot be null, empty or whitespace string.", nameof(@namespace));
    }
}
