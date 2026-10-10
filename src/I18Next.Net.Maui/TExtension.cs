using System;

using I18Next.Net.Xaml;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace I18Next.Net.Maui;

/// <summary>
///     Translates a key, e.g. <c>Text="{i18n:T menu.title}"</c>. The text updates when the language or the translations
///     of <see cref="I18NextXaml.Instance" /> change.
/// </summary>
[ContentProperty(nameof(Key))]
[AcceptEmptyServiceProvider]
public class TExtension : IMarkupExtension<BindingBase>
{
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

    public BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key))
            throw new InvalidOperationException("The key to be translated is missing.");

        var source = I18NextXaml.Source;
        var converter = new TranslationConverter(source, Key, Namespace, Args, Count);
        var sourceBinding = new Binding(nameof(TranslationSource.Language), BindingMode.OneWay, source: source);

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

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
    {
        return ProvideValue(serviceProvider);
    }
}
