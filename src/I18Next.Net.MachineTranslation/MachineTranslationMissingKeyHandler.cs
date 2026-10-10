using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Logging;
using I18Next.Net.Plugins;
using I18Next.Net.TranslationTrees;

namespace I18Next.Net.MachineTranslation;

/// <summary>
///     Translates keys missing in a language from the source language while developing. Every key is translated once per
///     handler, the result is passed to a callback or stored in an <see cref="InMemoryBackend" /> for the next lookups.
/// </summary>
public class MachineTranslationMissingKeyHandler : IMissingKeyHandler
{
    private static readonly string[] PluralCategories = ["zero", "one", "two", "few", "many", "other"];

    private readonly ITranslationBackend _backend;
    private readonly Func<MachineTranslatedKey, Task> _onTranslated;
    private readonly ConcurrentDictionary<(string Language, string Namespace, string Key), Lazy<Task>> _requests = new();
    private readonly IMachineTranslator _translator;

    /// <param name="translator">The machine translator.</param>
    /// <param name="backend">The backend the source language texts are loaded from.</param>
    /// <param name="sourceLanguage">The language missing keys are translated from.</param>
    /// <param name="onTranslated">Receives the translated keys, e.g. to save them.</param>
    public MachineTranslationMissingKeyHandler(IMachineTranslator translator, ITranslationBackend backend, string sourceLanguage,
        Func<MachineTranslatedKey, Task> onTranslated)
    {
        if (string.IsNullOrWhiteSpace(sourceLanguage))
            throw new ArgumentException("Source language cannot be null, empty or whitespace string.", nameof(sourceLanguage));

        _translator = translator ?? throw new ArgumentNullException(nameof(translator));
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _onTranslated = onTranslated ?? throw new ArgumentNullException(nameof(onTranslated));
        SourceLanguage = sourceLanguage;
    }

    /// <summary>
    ///     Stores the translated keys in <paramref name="target" />. When the target does not contain the namespace of a
    ///     language yet, it is filled with the namespace from <paramref name="backend" /> first, so the target can be put in
    ///     front of the backend in a <see cref="ChainedBackend" />.
    /// </summary>
    /// <param name="translator">The machine translator.</param>
    /// <param name="backend">The backend the source language texts and existing translations are loaded from.</param>
    /// <param name="sourceLanguage">The language missing keys are translated from.</param>
    /// <param name="target">The backend the translated keys are stored in.</param>
    public MachineTranslationMissingKeyHandler(IMachineTranslator translator, ITranslationBackend backend, string sourceLanguage, InMemoryBackend target)
        : this(translator, backend, sourceLanguage, CreateStore(target, backend))
    {
    }

    public string SourceLanguage { get; }

    public ILogger Logger { get; set; } = new TraceLogger();

    public string PluralSeparator { get; set; } = "_";

    /// <summary>
    ///     Lets the translation call wait until the missing key is translated. The waiting call still returns the
    ///     untranslated result, only the following calls use the translation.
    /// </summary>
    public bool WaitForTranslation { get; set; }

    public Task HandleMissingKeyAsync(object sender, MissingKeyEventArgs args)
    {
        if (args == null)
            throw new ArgumentNullException(nameof(args));

        if (IsSourceLanguage(args.Language) || args.Language is "cimode" or "dev")
            return Task.CompletedTask;

        var possibleKeys = args.PossibleKeys is { Length: > 0 } ? args.PossibleKeys : [args.Key];
        var requestKey = (args.Language, args.Namespace, possibleKeys[possibleKeys.Length - 1]);
        var request = _requests.GetOrAdd(requestKey, _ => new Lazy<Task>(() => Task.Run(() => TranslateAsync(sender, args.Language, args.Namespace, possibleKeys))))
            .Value;

        return WaitForTranslation ? request : Task.CompletedTask;
    }

    private static Func<MachineTranslatedKey, Task> CreateStore(InMemoryBackend target, ITranslationBackend backend)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        return async translation =>
        {
            IDictionary<string, string> existing = null;

            if (backend != target && !target.HasNamespace(translation.Language, translation.Namespace))
                existing = (await backend.LoadNamespaceAsync(translation.Language, translation.Namespace).ConfigureAwait(false))?.GetAllValues();

            lock (target)
            {
                if (existing != null && !target.HasNamespace(translation.Language, translation.Namespace))
                    target.AddTranslations(translation.Language, translation.Namespace, existing);

                target.AddTranslation(translation.Language, translation.Namespace, translation.Key, translation.Text);
            }
        };
    }

    private static string GetValue(ITranslationTree tree, string key)
    {
        try
        {
            return tree.GetValue(key, null);
        }
        catch (TranslationKeyInvalidException)
        {
            return null;
        }
    }

    private bool IsSourceLanguage(string language)
    {
        return string.Equals(language, SourceLanguage, StringComparison.OrdinalIgnoreCase)
               || string.Equals(BackendUtilities.GetLanguagePart(language), SourceLanguage, StringComparison.OrdinalIgnoreCase);
    }

    private async Task TranslateAsync(object sender, string language, string @namespace, string[] possibleKeys)
    {
        try
        {
            var sourceTree = await _backend.LoadNamespaceAsync(SourceLanguage, @namespace).ConfigureAwait(false);
            var (key, sourceText) = sourceTree == null ? default : FindSourceText(sourceTree, possibleKeys);

            if (string.IsNullOrEmpty(sourceText))
            {
                if (Logger.IsEnabled(LogLevel.Debug))
                    Logger.LogDebug("No source text in {sourceLanguage} to translate {namespace}:{key} into {language}.", SourceLanguage, @namespace,
                        possibleKeys[0], language);

                return;
            }

            var text = await _translator.TranslateAsync(sourceText, SourceLanguage, language).ConfigureAwait(false);

            await _onTranslated(new MachineTranslatedKey(language, @namespace, key, SourceLanguage, sourceText, text)).ConfigureAwait(false);

            (sender as DefaultTranslator)?.ClearCache(language, @namespace);
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "Machine translation of {namespace}:{key} into {language} failed.", @namespace, possibleKeys[0], language);
        }
    }

    private (string Key, string Text) FindSourceText(ITranslationTree sourceTree, string[] possibleKeys)
    {
        for (var i = possibleKeys.Length - 1; i >= 0; i--)
        {
            var key = possibleKeys[i];
            var text = GetValue(sourceTree, key);

            if (string.IsNullOrEmpty(text) && TryGetOtherPluralKey(key, out var otherKey))
                text = GetValue(sourceTree, otherKey);

            if (!string.IsNullOrEmpty(text))
                return (key, text);
        }

        return default;
    }

    private bool TryGetOtherPluralKey(string key, out string otherKey)
    {
        foreach (var category in PluralCategories)
        {
            var suffix = PluralSeparator + category;

            if (category != "other" && key.EndsWith(suffix, StringComparison.Ordinal))
            {
                otherKey = key.Substring(0, key.Length - suffix.Length) + PluralSeparator + "other";
                return true;
            }
        }

        otherKey = null;
        return false;
    }
}
