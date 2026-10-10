#if NET6_0_OR_GREATER
using System.Collections.Generic;

namespace I18Next.Net.AspNetCore;

/// <summary>
///     Restricts the languages and namespaces which can be requested from an I18Next endpoint.
/// </summary>
public abstract class I18NextEndpointOptions
{
    /// <summary>
    ///     The languages which can be requested, compared case insensitive. When empty, every language consisting of
    ///     letters, digits, dashes and underscores is allowed.
    /// </summary>
    public IList<string> Languages { get; set; } = [];

    /// <summary>
    ///     The namespaces which can be requested, compared case sensitive. When empty, every namespace consisting of
    ///     letters, digits, dashes, underscores and single dots is allowed.
    /// </summary>
    public IList<string> Namespaces { get; set; } = [];
}
#endif
