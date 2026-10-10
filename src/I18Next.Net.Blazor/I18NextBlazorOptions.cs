using System;
using System.Collections.Generic;

namespace I18Next.Net.Blazor;

public class I18NextBlazorOptions
{
    /// <summary>
    ///     The languages a user can choose. Detected and stored languages are matched against them, unknown languages are
    ///     replaced by the default language. All languages are allowed if empty.
    /// </summary>
    public IList<string> SupportedLanguages { get; set; } = [];

    /// <summary>
    ///     The namespaces loaded by <see cref="IBlazorI18Next.InitializeAsync" /> and before the language changes. Only the
    ///     default namespace and the fallback namespaces are loaded if empty.
    /// </summary>
    public IList<string> Namespaces { get; set; } = [];

    /// <summary>
    ///     The cookie the chosen language is stored in, using the format of the ASP.NET Core
    ///     <c>CookieRequestCultureProvider</c>. <c>null</c> disables the cookie.
    /// </summary>
    public string CookieName { get; set; } = ".AspNetCore.Culture";

    /// <summary>
    ///     The local storage key the chosen language is stored in, like the i18next browser language detector.
    ///     <c>null</c> disables the local storage.
    /// </summary>
    public string StorageKey { get; set; } = "i18nextLng";

    /// <summary>
    ///     The HTML tags without attributes the <see cref="Trans" /> component renders when <see cref="Trans.AllowHtml" /> is
    ///     enabled. Other markup is rendered as text.
    /// </summary>
    public ISet<string> AllowedHtmlTags { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "b", "br", "code", "em", "i", "p", "s", "small", "strong", "sub", "sup", "u"
    };
}
