using System;

namespace I18Next.Net.FluentValidation;

/// <summary>
///     Options of the FluentValidation integration of I18Next.
/// </summary>
public class I18NextFluentValidationOptions
{
    /// <summary>
    ///     The namespace containing the validation messages. Defaults to <c>validation</c>.
    /// </summary>
    public string Namespace
    {
        get;
        set => field = string.IsNullOrEmpty(value) ? throw new ArgumentException("Namespace cannot be null or empty.", nameof(value)) : value;
    } = "validation";

    /// <summary>
    ///     Prefix of the message keys, e.g. <c>fluent</c> to look up <c>NotEmptyValidator</c> as <c>fluent.NotEmptyValidator</c>.
    /// </summary>
    public string KeyPrefix { get; set; }

    /// <summary>
    ///     Translates the property names. The name of the <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute" />,
    ///     the <see cref="System.ComponentModel.DisplayNameAttribute" /> or the name of the property is used as the key.
    ///     FluentValidation's default display name is used if no translation exists. Defaults to <c>true</c>.
    /// </summary>
    public bool TranslateDisplayNames { get; set; } = true;

    /// <summary>
    ///     The namespace containing the property names. Uses the default namespace of I18Next if not set.
    /// </summary>
    public string DisplayNameNamespace { get; set; }
}
