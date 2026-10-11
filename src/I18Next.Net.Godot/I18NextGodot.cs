using System;
using System.Collections.Generic;
using System.Threading;

using Godot;

using I18Next.Net.Backends;

namespace I18Next.Net.Godot;

/// <summary>
///     Registers an I18Next instance with the Godot <see cref="TranslationServer" />, so <c>Tr()</c>, <c>TrN()</c> and auto
///     translated controls are translated by i18next. The Godot locale and the I18Next language are kept in sync.
/// </summary>
public static class I18NextGodot
{
    private static INotifyingTranslationBackend _backend;
    private static string _locale;
    private static I18NextLocaleNode _node;
    private static int _refreshPending;
    private static int _threadId;
    private static I18NextTranslation[] _translations = [];

    /// <summary>
    ///     The registered instance, also used by the <see cref="GodotObjectExtensions.T" /> extension method.
    /// </summary>
    public static II18Next Instance { get; private set; }

    /// <summary>
    ///     Adds a translation for each of the given languages to the <see cref="TranslationServer" /> and sets the Godot locale
    ///     to the language of the instance. Afterwards changing the language of the instance changes the Godot locale and
    ///     changing the Godot locale changes the language of the instance. Changed translations of an
    ///     <see cref="INotifyingTranslationBackend" /> translate all controls again. Call it on the main thread, registering
    ///     another instance replaces the previous one.
    /// </summary>
    /// <remarks>
    ///     Language changes and changed translations reported on other threads are applied on the main thread with a
    ///     deferred call.
    /// </remarks>
    /// <param name="i18Next">The I18Next instance.</param>
    /// <param name="languages">The languages to add to the translation server, e.g. <c>en</c> and <c>de</c>.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="i18Next" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">If no language is given.</exception>
    public static void Register(II18Next i18Next, params string[] languages)
    {
        if (i18Next == null)
            throw new ArgumentNullException(nameof(i18Next));

        if (languages == null || languages.Length == 0)
            throw new ArgumentException("At least one language is required.", nameof(languages));

        RemoveInstance();

        Instance = i18Next;
        _threadId = Thread.CurrentThread.ManagedThreadId;
        _translations = new I18NextTranslation[languages.Length];

        for (var i = 0; i < languages.Length; i++)
        {
            _translations[i] = new I18NextTranslation(i18Next, languages[i]);
            TranslationServer.AddTranslation(_translations[i]);
        }

        i18Next.LanguageChanged += OnLanguageChanged;
        _backend = i18Next.Backend as INotifyingTranslationBackend;

        if (_backend != null)
            _backend.TranslationsChanged += OnTranslationsChanged;

        if (Engine.GetMainLoop() is SceneTree tree)
        {
            _node = new I18NextLocaleNode { Name = "I18Next" };
            tree.Root.CallDeferred(Node.MethodName.AddChild, _node);
        }

        if (!ApplyLanguage(i18Next.Language))
            NotifyTranslationChanged();
    }

    /// <summary>
    ///     Removes the translations of the registered instance from the <see cref="TranslationServer" /> and stops keeping
    ///     the Godot locale and the language in sync.
    /// </summary>
    public static void Unregister()
    {
        if (RemoveInstance())
            NotifyTranslationChanged();
    }

    /// <summary>
    ///     Converts an i18next language like <c>de-DE</c> to a Godot locale like <c>de_DE</c>.
    /// </summary>
    /// <param name="language">The i18next language.</param>
    /// <returns>The Godot locale or <c>null</c> if <paramref name="language" /> is <c>null</c>.</returns>
    public static string ToLocale(string language)
    {
        return language?.Replace('-', '_');
    }

    /// <summary>
    ///     Converts a Godot locale like <c>de_DE</c> to an i18next language like <c>de-DE</c>.
    /// </summary>
    /// <param name="locale">The Godot locale.</param>
    /// <returns>The i18next language or <c>null</c> if <paramref name="locale" /> is <c>null</c>.</returns>
    public static string ToLanguage(string locale)
    {
        return locale?.Replace('_', '-');
    }

    internal static string Translate(II18Next i18Next, string language, string key, string context, int? count)
    {
        if (i18Next == null || string.IsNullOrEmpty(language) || string.IsNullOrEmpty(key))
            return null;

        Dictionary<string, object> args = null;

        if (count != null)
            args = new Dictionary<string, object> { ["count"] = count.Value };

        if (!string.IsNullOrEmpty(context))
            (args ??= [])["context"] = context;

        if (!i18Next.ExistsAsync(language, key, args).ConfigureAwait(false).GetAwaiter().GetResult())
            return null;

        return i18Next.T(language, key, args);
    }

    internal static bool SyncLanguage()
    {
        var i18Next = Instance;

        if (i18Next == null || Thread.CurrentThread.ManagedThreadId != _threadId)
            return false;

        var locale = TranslationServer.GetLocale();

        if (locale == _locale)
            return false;

        _locale = locale;

        if (!string.IsNullOrEmpty(i18Next.Language) && TranslationServer.StandardizeLocale(ToLocale(i18Next.Language)) == locale)
            return false;

        i18Next.Language = ToLanguage(locale);

        return true;
    }

    internal static void NotifyTranslationChanged()
    {
        Engine.GetMainLoop()?.Notification((int)MainLoop.NotificationTranslationChanged);
    }

    private static bool ApplyLanguage(string language)
    {
        if (string.IsNullOrEmpty(language))
            return false;

        var locale = TranslationServer.StandardizeLocale(ToLocale(language));
        _locale = locale;

        if (locale == TranslationServer.GetLocale())
            return false;

        TranslationServer.SetLocale(locale);

        return true;
    }

    private static void Refresh()
    {
        Interlocked.Exchange(ref _refreshPending, 0);

        if (Instance != null)
            NotifyTranslationChanged();
    }

    private static bool RemoveInstance()
    {
        var i18Next = Instance;

        if (i18Next == null)
            return false;

        Instance = null;
        _locale = null;
        i18Next.LanguageChanged -= OnLanguageChanged;

        if (_backend != null)
            _backend.TranslationsChanged -= OnTranslationsChanged;

        _backend = null;

        foreach (var translation in _translations)
            TranslationServer.RemoveTranslation(translation);

        _translations = [];

        if (_node != null && GodotObject.IsInstanceValid(_node))
            _node.QueueFree();

        _node = null;

        return true;
    }

    private static void OnLanguageChanged(object sender, LanguageChangedEventArgs e)
    {
        if (ReferenceEquals(sender, Instance))
            RunOnMainThread(() => ApplyLanguage(e.NewLanguage));
    }

    private static void OnTranslationsChanged(object sender, TranslationsChangedEventArgs e)
    {
        if (Thread.CurrentThread.ManagedThreadId == _threadId)
            Refresh();
        else if (Interlocked.Exchange(ref _refreshPending, 1) == 0)
            Callable.From(Refresh).CallDeferred();
    }

    private static void RunOnMainThread(Action action)
    {
        if (Thread.CurrentThread.ManagedThreadId == _threadId)
            action();
        else
            Callable.From(action).CallDeferred();
    }
}
