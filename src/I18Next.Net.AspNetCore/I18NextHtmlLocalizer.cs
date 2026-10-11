using System;

using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;

namespace I18Next.Net.AspNetCore;

/// <summary>
///     An <see cref="IHtmlLocalizer" /> which passes the arguments to the wrapped string localizer, so they are
///     interpolated by I18Next (e.g. <c>{{name}}</c>) instead of being formatted with
///     <see cref="string.Format(string, object[])" />. The I18Next string localizer uses the first argument as the
///     interpolation arguments.
/// </summary>
/// <param name="localizer">The string localizer which translates the keys.</param>
public class I18NextHtmlLocalizer(IStringLocalizer localizer) : HtmlLocalizer(localizer)
{
    private readonly IStringLocalizer _localizer = localizer;

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">If <paramref name="name" /> is <c>null</c>.</exception>
    public override LocalizedHtmlString this[string name, params object[] arguments]
    {
        get
        {
            return name == null ? throw new ArgumentNullException(nameof(name)) : ToHtmlString(_localizer[name, arguments]);
        }
    }
}
