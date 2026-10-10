using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

using I18Next.Net.Backends;
using I18Next.Net.Logging;
using I18Next.Net.Plugins;

using Shouldly;

using Xunit;

namespace I18Next.Net.Wpf.Tests;

public class TExtensionFixture : IClassFixture<StaDispatcher>, IDisposable
{
    private const string Namespaces =
        """xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:i18n="https://github.com/DarkLiKally/I18Next.Net" """;

    private readonly NotifyingBackend _backend = new();
    private readonly StaDispatcher _dispatcher;
    private readonly I18NextNet _i18Next;

    public TExtensionFixture(StaDispatcher dispatcher)
    {
        _dispatcher = dispatcher;

        _backend.AddTranslation("en", "translation", "menu.title", "Menu");
        _backend.AddTranslation("en", "translation", "total", "Total: {{value}}");
        _backend.AddTranslation("en", "translation", "item_one", "{{count}} item of {{Name}}");
        _backend.AddTranslation("en", "translation", "item_other", "{{count}} items of {{Name}}");
        _backend.AddTranslation("en", "common", "menu.title", "Common menu");
        _backend.AddTranslation("de", "translation", "menu.title", "Menü");

        var logger = new TraceLogger();
        var pluralResolver = new DefaultPluralResolver { JsonFormatVersion = JsonFormat.Version4 };
        var translator = new DefaultTranslator(_backend, logger, pluralResolver, new DefaultInterpolator(logger));

        _i18Next = new I18NextNet(_backend, translator) { Language = "en" };
        _dispatcher.Invoke(() => I18NextXaml.Instance = _i18Next);
    }

    public void Dispose()
    {
        _dispatcher.Invoke(() => I18NextXaml.Instance = null);
    }

    private static T Load<T>(string xaml, object dataContext = null)
        where T : FrameworkElement
    {
        var element = (T)XamlReader.Parse(xaml);
        element.DataContext = dataContext;
        StaDispatcher.Flush();

        return element;
    }

    private static TextBlock LoadTextBlock(string attributes, object dataContext = null)
    {
        return Load<TextBlock>($"<TextBlock {Namespaces} {attributes} />", dataContext);
    }

    [Fact]
    public void Key_ShouldTranslateAndUpdateWhenLanguageChanges()
    {
        _dispatcher.Invoke(() =>
        {
            var text = LoadTextBlock("""Text="{i18n:T menu.title}" """);

            text.Text.ShouldBe("Menu");

            _i18Next.Language = "de";
            StaDispatcher.Flush();

            text.Text.ShouldBe("Menü");
        });
    }

    [Fact]
    public void Namespace_ShouldTranslateFromNamespace()
    {
        _dispatcher.Invoke(() =>
        {
            LoadTextBlock("""Text="{i18n:T menu.title, Namespace=common}" """).Text.ShouldBe("Common menu");
            LoadTextBlock("""Text="{i18n:T common:menu.title}" """).Text.ShouldBe("Common menu");
        });
    }

    [Fact]
    public void ArgsAndCount_Bindings_ShouldTranslateWithBoundValues()
    {
        _dispatcher.Invoke(() =>
        {
            var model = new ViewModel { User = new User("Jane"), Count = 3 };
            var text = LoadTextBlock("""Text="{i18n:T item, Args={Binding User}, Count={Binding Count}}" """, model);

            text.Text.ShouldBe("3 items of Jane");

            model.Count = 1;
            model.User = new User("John");
            StaDispatcher.Flush();

            text.Text.ShouldBe("1 item of John");
        });
    }

    [Fact]
    public void ArgsAndCount_Values_ShouldTranslate()
    {
        _dispatcher.Invoke(() =>
        {
            LoadTextBlock("""Text="{i18n:T total, Args=many}" """).Text.ShouldBe("Total: many");
            LoadTextBlock("""Text="{i18n:T item, Count=2, Args={Binding User}}" """, new ViewModel { User = new User("Jane") })
                .Text.ShouldBe("2 items of Jane");
        });
    }

    [Fact]
    public void TwoWayProperty_ShouldBeBoundOneWay()
    {
        _dispatcher.Invoke(() =>
        {
            var textBox = Load<TextBox>($"""<TextBox {Namespaces} Text="{"{"}i18n:T menu.title{"}"}" />""");

            textBox.Text.ShouldBe("Menu");
            BindingOperations.GetBinding(textBox, TextBox.TextProperty).Mode.ShouldBe(BindingMode.OneWay);
        });
    }

    [Fact]
    public void StyleSetter_ShouldTranslateAndUpdate()
    {
        _dispatcher.Invoke(() =>
        {
            var label = Load<Label>($"""
                <Label {Namespaces}>
                  <Label.Style>
                    <Style TargetType="Label">
                      <Setter Property="Content" Value="{"{"}i18n:T menu.title{"}"}" />
                    </Style>
                  </Label.Style>
                </Label>
                """);

            label.Content.ShouldBe("Menu");

            _i18Next.Language = "de";
            StaDispatcher.Flush();

            label.Content.ShouldBe("Menü");
        });
    }

    [Fact]
    public void TranslationsChanged_OnOtherThread_ShouldUpdateOnDispatcher()
    {
        _dispatcher.Invoke(() =>
        {
            var text = LoadTextBlock("""Text="{i18n:T menu.title}" """);
            text.Text.ShouldBe("Menu");

            _backend.AddTranslation("en", "translation", "menu.title", "Main menu");

            var thread = new Thread(() => _backend.RaiseTranslationsChanged("en", "translation"));
            thread.Start();
            thread.Join();
            StaDispatcher.Flush();

            text.Text.ShouldBe("Main menu");
        });
    }

    [Fact]
    public void Instance_Changed_ShouldTranslateAgain()
    {
        _dispatcher.Invoke(() =>
        {
            var text = LoadTextBlock("""Text="{i18n:T menu.title}" """);

            var backend = new InMemoryBackend();
            backend.AddTranslation("en", "translation", "menu.title", "Other menu");
            I18NextXaml.Instance = new I18NextNet(backend, new DefaultTranslator(backend)) { Language = "en" };
            StaDispatcher.Flush();

            text.Text.ShouldBe("Other menu");

            I18NextXaml.Instance = null;
            StaDispatcher.Flush();

            text.Text.ShouldBe("menu.title");
        });
    }

    [Fact]
    public void CompiledXaml_ShouldTranslateAndUpdate()
    {
        _dispatcher.Invoke(() =>
        {
            var model = new ViewModel { User = new User("Jane"), Count = 2 };
            var view = new TranslatedView { DataContext = model };
            StaDispatcher.Flush();

            view.Title.ShouldBe("Menu");
            view.Items.ShouldBe("2 items of Jane");

            model.Count = 1;
            StaDispatcher.Flush();

            view.Items.ShouldBe("1 item of Jane");
        });
    }

    [Fact]
    public void ProvideValue_ShouldCreateOneWayBindings()
    {
        _dispatcher.Invoke(() =>
        {
            Should.Throw<InvalidOperationException>(() => new TExtension().ProvideValue(null));

            var binding = new TExtension("menu.title").ProvideValue(null).ShouldBeOfType<Binding>();
            binding.Mode.ShouldBe(BindingMode.OneWay);
            Should.Throw<NotSupportedException>(() => binding.Converter.ConvertBack("Menu", typeof(string), null, null));

            var multiBinding = new TExtension("item") { Count = new Binding("Count") }.ProvideValue(null).ShouldBeOfType<MultiBinding>();
            multiBinding.Mode.ShouldBe(BindingMode.OneWay);
            multiBinding.Bindings.Count.ShouldBe(2);
            Should.Throw<NotSupportedException>(() => multiBinding.Converter.ConvertBack("Menu", [typeof(object)], null, null));
        });
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
