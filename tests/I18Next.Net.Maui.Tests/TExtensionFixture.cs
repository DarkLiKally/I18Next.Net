using System;
using System.Runtime.CompilerServices;
using System.Threading;

using I18Next.Net.Backends;
using I18Next.Net.Logging;
using I18Next.Net.Plugins;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

using Shouldly;

using Xunit;

namespace I18Next.Net.Maui.Tests;

[Collection(nameof(I18NextXaml))]
public class TExtensionFixture : IDisposable
{
    private const string Namespaces =
        """xmlns="http://schemas.microsoft.com/dotnet/2021/maui" xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml" xmlns:i18n="https://github.com/DarkLiKally/I18Next.Net" """;

    private readonly NotifyingBackend _backend = new();
    private readonly I18NextNet _i18Next;

    public TExtensionFixture()
    {
        TestDispatcher.Use();
        SynchronizationContext.SetSynchronizationContext(null);

        _backend.AddTranslation("en", "translation", "menu.title", "Menu");
        _backend.AddTranslation("en", "translation", "greeting", "Hello {{Name}}");
        _backend.AddTranslation("en", "translation", "total", "Total: {{value}}");
        _backend.AddTranslation("en", "translation", "item_one", "{{count}} item of {{Name}}");
        _backend.AddTranslation("en", "translation", "item_other", "{{count}} items of {{Name}}");
        _backend.AddTranslation("en", "common", "menu.title", "Common menu");
        _backend.AddTranslation("de", "translation", "menu.title", "Menü");

        var logger = new TraceLogger();
        var pluralResolver = new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 };
        var translator = new DefaultTranslator(_backend, logger, pluralResolver, new DefaultInterpolator(logger));

        _i18Next = new I18NextNet(_backend, translator) { Language = "en" };
        I18NextXaml.Instance = _i18Next;
    }

    public void Dispose()
    {
        I18NextXaml.Instance = null;
    }

    private static Label LoadLabel(string attributes, object bindingContext = null)
    {
        return new Label { BindingContext = bindingContext }.LoadFromXaml($"<Label {Namespaces} {attributes} />");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] CreateTranslatedLabels()
    {
        var model = new ViewModel { User = new User("Jane"), Count = 2 };

        return
        [
            new WeakReference(LoadLabel("""Text="{i18n:T menu.title}" """)),
            new WeakReference(LoadLabel("""Text="{i18n:T item, Args={Binding User}, Count={Binding Count}}" """, model)),
            new WeakReference(new TranslatedView { BindingContext = model })
        ];
    }

    [Fact]
    public void Key_ShouldTranslateAndUpdateWhenLanguageChanges()
    {
        var label = LoadLabel("""Text="{i18n:T menu.title}" """);

        label.Text.ShouldBe("Menu");

        _i18Next.Language = "de";

        label.Text.ShouldBe("Menü");
    }

    [Fact]
    public void Namespace_ShouldTranslateFromNamespace()
    {
        LoadLabel("""Text="{i18n:T menu.title, Namespace=common}" """).Text.ShouldBe("Common menu");
        LoadLabel("""Text="{i18n:T common:menu.title}" """).Text.ShouldBe("Common menu");
    }

    [Fact]
    public void Args_Binding_ShouldTranslateWithBoundValue()
    {
        var model = new ViewModel { User = new User("Jane"), Total = 42 };
        var greeting = LoadLabel("""Text="{i18n:T greeting, Args={Binding User}}" """, model);
        var total = LoadLabel("""Text="{i18n:T total, Args={Binding Total}}" """, model);

        greeting.Text.ShouldBe("Hello Jane");
        total.Text.ShouldBe("Total: 42");

        model.User = new User("John");
        model.Total = 7;

        greeting.Text.ShouldBe("Hello John");
        total.Text.ShouldBe("Total: 7");
    }

    [Fact]
    public void Args_Value_ShouldBeAvailableAsValue()
    {
        LoadLabel("""Text="{i18n:T total, Args=many}" """).Text.ShouldBe("Total: many");
    }

    [Fact]
    public void Count_Binding_ShouldResolvePlural()
    {
        var model = new ViewModel { User = new User("Jane"), Count = 3 };
        var label = LoadLabel("""Text="{i18n:T item, Args={Binding User}, Count={Binding Count}}" """, model);

        label.Text.ShouldBe("3 items of Jane");

        model.Count = 1;

        label.Text.ShouldBe("1 item of Jane");
    }

    [Fact]
    public void Count_Value_ShouldResolvePlural()
    {
        var label = LoadLabel("""Text="{i18n:T item, Count=1, Args={Binding User}}" """, new ViewModel { User = new User("Jane") });

        label.Text.ShouldBe("1 item of Jane");
    }

    [Fact]
    public void Binding_WithoutBindingContext_ShouldTranslateWithoutArgs()
    {
        LoadLabel("""Text="{i18n:T greeting, Args={Binding User}}" """).Text.ShouldBe("Hello ");
    }

    [Fact]
    public void TranslationsChanged_ShouldTranslateAgain()
    {
        var label = LoadLabel("""Text="{i18n:T menu.title}" """);
        label.Text.ShouldBe("Menu");

        _backend.AddTranslation("en", "translation", "menu.title", "Main menu");
        _backend.RaiseTranslationsChanged("en", "translation");

        label.Text.ShouldBe("Main menu");
    }

    [Fact]
    public void Instance_Changed_ShouldTranslateAgain()
    {
        var label = LoadLabel("""Text="{i18n:T menu.title}" """);

        var backend = new InMemoryBackend();
        backend.AddTranslation("en", "translation", "menu.title", "Other menu");
        I18NextXaml.Instance = new I18NextNet(backend, new DefaultTranslator(backend)) { Language = "en" };

        label.Text.ShouldBe("Other menu");

        I18NextXaml.Instance = null;

        label.Text.ShouldBe("menu.title");
    }

    [Fact]
    public void ElementSyntax_ShouldTranslate()
    {
        var label = new Label().LoadFromXaml($"<Label {Namespaces}><Label.Text><i18n:T Key=\"menu.title\" /></Label.Text></Label>");

        label.Text.ShouldBe("Menu");
    }

    [Fact]
    public void StyleSetter_ShouldTranslateAndUpdate()
    {
        var xaml = $"""
            <Label {Namespaces}>
              <Label.Style>
                <Style TargetType="Label">
                  <Setter Property="Text" Value="{"{"}i18n:T menu.title{"}"}" />
                </Style>
              </Label.Style>
            </Label>
            """;
        var label = new Label().LoadFromXaml(xaml);

        label.Text.ShouldBe("Menu");

        _i18Next.Language = "de";

        label.Text.ShouldBe("Menü");
    }

    [Fact]
    public void CompiledXaml_ShouldTranslateAndUpdate()
    {
        var model = new ViewModel { User = new User("Jane"), Count = 2 };
        var view = new TranslatedView { BindingContext = model };

        view.TitleText.ShouldBe("Menu");
        view.ItemsText.ShouldBe("2 items of Jane");

        model.Count = 1;

        view.ItemsText.ShouldBe("1 item of Jane");

        _i18Next.Language = "de";

        view.TitleText.ShouldBe("Menü");
    }

    [Fact]
    public void Views_ShouldNotBeKeptAliveBySource()
    {
        var references = CreateTranslatedLabels();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        references.ShouldAllBe(r => !r.IsAlive);
        _i18Next.Language = "de";
    }

    [Fact]
    public void ProvideValue_WithoutKey_ShouldThrow()
    {
        Should.Throw<InvalidOperationException>(() => new TExtension().ProvideValue(null));
    }

    [Fact]
    public void ProvideValue_ShouldCreateOneWayBindings()
    {
        var binding = new TExtension { Key = "menu.title" }.ProvideValue(null).ShouldBeOfType<Binding>();
        binding.Mode.ShouldBe(BindingMode.OneWay);
        Should.Throw<NotSupportedException>(() => binding.Converter.ConvertBack("Menu", typeof(string), null, null));

        var multiBinding = new TExtension { Key = "item", Count = new Binding("Count") }.ProvideValue(null).ShouldBeOfType<MultiBinding>();
        multiBinding.Mode.ShouldBe(BindingMode.OneWay);
        multiBinding.Bindings.Count.ShouldBe(2);
        Should.Throw<NotSupportedException>(() => multiBinding.Converter.ConvertBack("Menu", [typeof(object)], null, null));
    }

    private sealed class NotifyingBackend : InMemoryBackend, INotifyingTranslationBackend
    {
        public event EventHandler<TranslationsChangedEventArgs> TranslationsChanged;

        public void RaiseTranslationsChanged(string language, string ns)
        {
            TranslationsChanged?.Invoke(this, new TranslationsChangedEventArgs(language, ns));
        }
    }
}
