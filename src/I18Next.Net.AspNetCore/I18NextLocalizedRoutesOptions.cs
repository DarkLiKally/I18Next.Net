using System.Collections.Generic;

using Microsoft.AspNetCore.Http;

namespace I18Next.Net.AspNetCore;

/// <summary>
///     Options of localized routes like <c>/de/produkte/42</c> and <c>/en/products/42</c>.
/// </summary>
public class I18NextLocalizedRoutesOptions
{
    /// <summary>
    ///     The languages used as path prefix, e.g. <c>en</c> and <c>de</c>. At least one language is required.
    /// </summary>
    public IList<string> Languages { get; set; } = [];

    /// <summary>
    ///     The language used when no other language matches. Defaults to the first of the <see cref="Languages" />.
    /// </summary>
    public string DefaultLanguage { get; set; }

    /// <summary>
    ///     The namespace containing the translated path segments keyed by the segment of the canonical route, e.g.
    ///     <c>{ "products": "produkte" }</c> in <c>de/routes.json</c>.
    /// </summary>
    public string Namespace { get; set; } = "routes";

    /// <summary>
    ///     Defines how requests without a language prefix are handled.
    /// </summary>
    public MissingLanguagePrefixBehavior MissingLanguagePrefix { get; set; } = MissingLanguagePrefixBehavior.None;

    /// <summary>
    ///     Paths which are never redirected to a language prefix, e.g. <c>/api</c> or <c>/locales</c>.
    /// </summary>
    public IList<PathString> ExcludedPaths { get; set; } = [];
}
