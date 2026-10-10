using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace I18Next.Net.FluentValidation;

/// <summary>
///     Display name resolver of FluentValidation which translates the property names using I18Next.
/// </summary>
public class I18NextDisplayNameResolver
{
    private readonly II18Next _i18Next;

    /// <summary>
    ///     Constructor using the default options.
    /// </summary>
    /// <param name="i18Next">The I18Next instance used to translate the property names.</param>
    public I18NextDisplayNameResolver(II18Next i18Next)
        : this(i18Next, new I18NextFluentValidationOptions())
    {
    }

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="i18Next">The I18Next instance used to translate the property names.</param>
    /// <param name="options">The options of the FluentValidation integration.</param>
    public I18NextDisplayNameResolver(II18Next i18Next, I18NextFluentValidationOptions options)
    {
        _i18Next = i18Next ?? throw new ArgumentNullException(nameof(i18Next));
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    ///     The options of the FluentValidation integration.
    /// </summary>
    public I18NextFluentValidationOptions Options { get; }

    /// <summary>
    ///     Translates the name of the member. Can be assigned to <c>ValidatorOptions.Global.DisplayNameResolver</c>.
    /// </summary>
    /// <param name="type">The type containing the member.</param>
    /// <param name="memberInfo">The validated member.</param>
    /// <param name="expression">The expression of the rule.</param>
    /// <returns>The translated name or null if no translation exists.</returns>
    public string Resolve(Type type, MemberInfo memberInfo, LambdaExpression expression)
    {
        if (memberInfo == null || !Options.TranslateDisplayNames)
            return null;

        var displayAttribute = memberInfo.GetCustomAttribute<DisplayAttribute>();
        var key = displayAttribute?.ResourceType == null ? displayAttribute?.Name : null;

        if (string.IsNullOrEmpty(key))
            key = memberInfo.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;

        if (string.IsNullOrEmpty(key))
            key = memberInfo.Name;

        var language = TranslationHelper.GetLanguage(_i18Next);
        var qualifiedKey = TranslationHelper.QualifyKey(Options.DisplayNameNamespace, key);

        return TranslationHelper.Exists(_i18Next, language, qualifiedKey) ? _i18Next.T(language, qualifiedKey) : null;
    }
}
