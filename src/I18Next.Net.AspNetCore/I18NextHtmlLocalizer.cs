using System;

using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;

namespace I18Next.Net.AspNetCore;

/// <inheritdoc />
/// <inheritdoc />
public class I18NextHtmlLocalizer(IStringLocalizer localizer) : HtmlLocalizer(localizer)
{
    private readonly IStringLocalizer _localizer = localizer;

    /// <inheritdoc />
    public override LocalizedHtmlString this[string name, params object[] arguments]
    {
        get
        {
            return name == null ? throw new ArgumentNullException(nameof(name)) : ToHtmlString(_localizer[name, arguments]);
        }
    }
}
