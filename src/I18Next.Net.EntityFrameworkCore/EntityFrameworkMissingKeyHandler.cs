using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using I18Next.Net.Plugins;

using Microsoft.EntityFrameworkCore;

namespace I18Next.Net.EntityFrameworkCore;

/// <summary>
///     Adds missing keys without a translation to the database, so they can be translated later, similar to the
///     saveMissing option of i18next. Each key is added once, keys exceeding the maximum lengths of
///     <see cref="TranslationEntry" /> are ignored. Meant for development and staging environments, as every requested key
///     is added.
/// </summary>
/// <typeparam name="TContext">The type of the context.</typeparam>
public class EntityFrameworkMissingKeyHandler<TContext> : IMissingKeyHandler
    where TContext : DbContext
{
    private readonly IDbContextFactory<TContext> _contextFactory;
    private readonly ConcurrentDictionary<(string Language, string Namespace, string Key), bool> _handledKeys = new();

    /// <summary>
    ///     Creates a handler adding the missing keys to the database.
    /// </summary>
    /// <param name="contextFactory">The factory creating a context for every added key.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="contextFactory" /> is <c>null</c>.</exception>
    public EntityFrameworkMissingKeyHandler(IDbContextFactory<TContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    /// <summary>
    ///     The language missing keys are added for, e.g. the development language. <c>null</c> adds them for the language
    ///     they are missing in.
    /// </summary>
    public string Language { get; set; }

    /// <summary>
    ///     Adds the missing key without a translation to the database unless it exists already. Keys added by other
    ///     processes at the same time are ignored. If adding the key fails, the exception is thrown and the key is added
    ///     again the next time it is missing.
    /// </summary>
    /// <param name="sender">The translator which misses the key.</param>
    /// <param name="args">The language, namespace and key which are missing.</param>
    /// <returns>A task which completes when the key is added.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="args" /> is <c>null</c>.</exception>
    public async Task HandleMissingKeyAsync(object sender, MissingKeyEventArgs args)
    {
        if (args == null)
            throw new ArgumentNullException(nameof(args));

        var handledKey = (Language: Language ?? args.Language, args.Namespace, args.Key);

        if (!CanBeStored(handledKey.Language, handledKey.Namespace, handledKey.Key) || !_handledKeys.TryAdd(handledKey, true))
            return;

        try
        {
            await AddEntryAsync(handledKey.Language, handledKey.Namespace, handledKey.Key).ConfigureAwait(false);
        }
        catch
        {
            _handledKeys.TryRemove(handledKey, out _);
            throw;
        }
    }

    private async Task AddEntryAsync(string language, string @namespace, string key)
    {
        using var context = _contextFactory.CreateDbContext();

        if (await ExistsAsync(context, language, @namespace, key).ConfigureAwait(false))
            return;

        context.Add(new TranslationEntry { Language = language, Namespace = @namespace, Key = key });

        try
        {
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            if (!await ExistsAsync(context, language, @namespace, key).ConfigureAwait(false))
                throw;
        }
    }

    private static Task<bool> ExistsAsync(TContext context, string language, string @namespace, string key)
    {
        return context.Set<TranslationEntry>().AnyAsync(e => e.Language == language && e.Namespace == @namespace && e.Key == key);
    }

    private static bool CanBeStored(string language, string @namespace, string key)
    {
        return !string.IsNullOrWhiteSpace(language) && language.Length <= TranslationEntry.LanguageMaxLength &&
               !string.IsNullOrWhiteSpace(@namespace) && @namespace.Length <= TranslationEntry.NamespaceMaxLength &&
               !string.IsNullOrWhiteSpace(key) && key.Length <= TranslationEntry.KeyMaxLength;
    }
}
