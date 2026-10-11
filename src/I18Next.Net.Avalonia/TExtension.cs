using System;

using Avalonia.Data;
using Avalonia.Markup.Xaml;

using I18Next.Net.Xaml;

namespace I18Next.Net.Avalonia;

/// <summary>
///     Translates a key, e.g. <c>Text="{i18n:T menu.title}"</c>. The text updates when the language or the translations
///     of <see cref="I18NextXaml.Instance" /> change.
/// </summary>
public class TExtension : MarkupExtension
{
    /// <summary>
    ///     Creates an extension without a key, which has to be set with <see cref="Key" />.
    /// </summary>
    public TExtension()
    {
    }

    /// <summary>
    ///     Creates an extension translating the given key, e.g. <c>{i18n:T menu.title}</c>.
    /// </summary>
    /// <param name="key">The key to be translated.</param>
    public TExtension(string key)
    {
        Key = key;
    }

    /// <summary>
    ///     The arguments used to translate the key, either a value or a binding. Values like strings, numbers and dates are
    ///     available as <c>{{value}}</c>, objects and dictionaries provide their properties.
    /// </summary>
    public object Args { get; set; }

    /// <summary>
    ///     The count used to resolve plurals, either a value or a binding.
    /// </summary>
    public object Count { get; set; }

    /// <summary>
    ///     The key to be translated.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    ///     The namespace to translate the key from instead of the default namespace.
    /// </summary>
    public string Namespace { get; set; }

    /// <summary>
    ///     Creates a one way binding which translates the key with <see cref="I18NextXaml.Instance" /> and updates when the
    ///     language or the translations change. When <see cref="Args" /> or <see cref="Count" /> is a binding, a multi
    ///     binding also updates when their values change.
    /// </summary>
    /// <param name="serviceProvider">The service provider of the XAML parser.</param>
    /// <returns>The binding.</returns>
    /// <exception cref="InvalidOperationException">If <see cref="Key" /> is not set.</exception>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key))
            throw new InvalidOperationException("The key to be translated is missing.");

        var source = I18NextXaml.Source;
        var converter = new TranslationConverter(source, Key, Namespace, Args, Count);
        var sourceBinding = new ReflectionBinding(nameof(TranslationSource.Version)) { Source = source, Mode = BindingMode.OneWay };

        if (Args is not BindingBase && Count is not BindingBase)
        {
            sourceBinding.Converter = converter;

            return sourceBinding;
        }

        var binding = new MultiBinding { Converter = converter, Mode = BindingMode.OneWay };
        binding.Bindings.Add(sourceBinding);

        if (Args is BindingBase argsBinding)
            binding.Bindings.Add(argsBinding);

        if (Count is BindingBase countBinding)
            binding.Bindings.Add(countBinding);

        return binding;
    }
}
