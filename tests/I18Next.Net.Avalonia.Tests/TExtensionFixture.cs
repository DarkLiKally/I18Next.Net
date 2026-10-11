using System;
using System.Threading;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

using I18Next.Net.Backends;
using I18Next.Net.Logging;
using I18Next.Net.Plugins;

using Shouldly;

using Xunit;

namespace I18Next.Net.Avalonia.Tests;

public class TExtensionFixture : IClassFixture<AvaloniaSession>, IDisposable
{
    private const string Namespaces = """xmlns="https://github.com/avaloniaui" xmlns:i18n="https://github.com/DarkLiKally/I18Next.Net" """;

    private readonly NotifyingBackend _backend = new();
    private readonly I18NextNet _i18Next;
    private readonly AvaloniaSession _session;

    public TExtensionFixture(AvaloniaSession session)
    {
        _session = session;

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
        _session.Invoke(() => I18NextXaml.Instance = _i18Next);
    }

    public void Dispose()
    {
        _session.Invoke(() => I18NextXaml.Instance = null);
    }

    private static T Load<T>(string xaml, object dataContext = null)
        where T : Control
    {
        var element = AvaloniaRuntimeXamlLoader.Parse<T>(xaml, typeof(TExtensionFixture).Assembly);
        element.DataContext = dataContext;
        AvaloniaSession.Flush();

        return element;
    }

    private static TextBlock LoadTextBlock(string attributes, object dataContext = null)
    {
        return Load<TextBlock>($"<TextBlock {Namespaces} {attributes} />", dataContext);
    }

    [Fact]
    public void Key_ShouldTranslateAndUpdateWhenLanguageChanges()
    {
        _session.Invoke(() =>
        {
            var text = LoadTextBlock("""Text="{i18n:T menu.title}" """);

            text.Text.ShouldBe("Menu");

            _i18Next.Language = "de";
            AvaloniaSession.Flush();

            text.Text.ShouldBe("Menü");
        });
    }

    [Fact]
    public void Namespace_ShouldTranslateFromNamespace()
    {
        _session.Invoke(() =>
        {
            LoadTextBlock("""Text="{i18n:T menu.title, Namespace=common}" """).Text.ShouldBe("Common menu");
            LoadTextBlock("""Text="{i18n:T common:menu.title}" """).Text.ShouldBe("Common menu");
        });
    }

    [Fact]
    public void ArgsAndCount_Bindings_ShouldTranslateWithBoundValues()
    {
        _session.Invoke(() =>
        {
            var model = new ViewModel { User = new User("Jane"), Count = 3 };
            var text = LoadTextBlock("""Text="{i18n:T item, Args={Binding User}, Count={Binding Count}}" """, model);

            text.Text.ShouldBe("3 items of Jane");

            model.Count = 1;
            model.User = new User("John");
            AvaloniaSession.Flush();

            text.Text.ShouldBe("1 item of John");
        });
    }

    [Fact]
    public void ArgsAndCount_Values_ShouldTranslate()
    {
        _session.Invoke(() =>
        {
            LoadTextBlock("""Text="{i18n:T total, Args=many}" """).Text.ShouldBe("Total: many");
            LoadTextBlock("""Text="{i18n:T item, Count=2, Args={Binding User}}" """, new ViewModel { User = new User("Jane") })
                .Text.ShouldBe("2 items of Jane");
        });
    }

    [Fact]
    public void TwoWayProperty_ShouldBeBoundOneWay()
    {
        _session.Invoke(() =>
        {
            var textBox = Load<TextBox>($"""<TextBox {Namespaces} Text="{"{"}i18n:T menu.title{"}"}" />""");

            textBox.Text.ShouldBe("Menu");

            textBox.Text = "Edited";
            _i18Next.Language = "de";
            AvaloniaSession.Flush();

            textBox.Text.ShouldBe("Menü");
        });
    }

    [Fact]
    public void StyleSetter_ShouldTranslateAndUpdate()
    {
        _session.Invoke(() =>
        {
            var panel = Load<StackPanel>($"""
                <StackPanel {Namespaces}>
                  <StackPanel.Styles>
                    <Style Selector="TextBlock">
                      <Setter Property="Text" Value="{"{"}i18n:T menu.title{"}"}" />
                    </Style>
                  </StackPanel.Styles>
                  <TextBlock />
                </StackPanel>
                """);
            var window = new Window { Content = panel };
            window.Show();
            AvaloniaSession.Flush();

            var text = (TextBlock)panel.Children[0];
            text.Text.ShouldBe("Menu");

            _i18Next.Language = "de";
            AvaloniaSession.Flush();

            text.Text.ShouldBe("Menü");
            window.Close();
        });
    }

    [Fact]
    public void TranslationsChanged_OnOtherThread_ShouldUpdateOnUiThread()
    {
        _session.Invoke(() =>
        {
            var text = LoadTextBlock("""Text="{i18n:T menu.title}" """);
            text.Text.ShouldBe("Menu");

            _backend.AddTranslation("en", "translation", "menu.title", "Main menu");

            var thread = new Thread(() => _backend.RaiseTranslationsChanged("en", "translation"));
            thread.Start();
            thread.Join();
            AvaloniaSession.Flush();

            text.Text.ShouldBe("Main menu");
        });
    }

    [Fact]
    public void Instance_Changed_ShouldTranslateAgain()
    {
        _session.Invoke(() =>
        {
            var text = LoadTextBlock("""Text="{i18n:T menu.title}" """);

            var backend = new InMemoryBackend();
            backend.AddTranslation("en", "translation", "menu.title", "Other menu");
            I18NextXaml.Instance = new I18NextNet(backend, new DefaultTranslator(backend)) { Language = "en" };
            AvaloniaSession.Flush();

            text.Text.ShouldBe("Other menu");

            I18NextXaml.Instance = null;
            AvaloniaSession.Flush();

            text.Text.ShouldBe("menu.title");
        });
    }

    [Fact]
    public void CompiledXaml_ShouldTranslateAndUpdate()
    {
        _session.Invoke(() =>
        {
            var model = new ViewModel { User = new User("Jane"), Count = 2 };
            var view = new TranslatedView { DataContext = model };
            AvaloniaSession.Flush();

            view.Title.ShouldBe("Menu");
            view.Items.ShouldBe("2 items of Jane");

            model.Count = 1;
            AvaloniaSession.Flush();

            view.Items.ShouldBe("1 item of Jane");

            _i18Next.Language = "de";
            AvaloniaSession.Flush();

            view.Title.ShouldBe("Menü");
        });
    }

    [Fact]
    public void ProvideValue_ShouldCreateOneWayBindings()
    {
        _session.Invoke(() =>
        {
            Should.Throw<InvalidOperationException>(() => new TExtension().ProvideValue(null));

            var binding = new TExtension("menu.title").ProvideValue(null).ShouldBeOfType<ReflectionBinding>();
            binding.Mode.ShouldBe(BindingMode.OneWay);
            Should.Throw<NotSupportedException>(() => binding.Converter.ConvertBack("Menu", typeof(string), null, null));

            var multiBinding = new TExtension("item") { Count = new ReflectionBinding("Count") }.ProvideValue(null).ShouldBeOfType<MultiBinding>();
            multiBinding.Mode.ShouldBe(BindingMode.OneWay);
            multiBinding.Bindings.Count.ShouldBe(2);
            multiBinding.Converter.Convert([0, AvaloniaProperty.UnsetValue], typeof(string), null, null).ShouldBe("item");
        });
    }

    [Fact]
    public void UseI18Next_ShouldSetInstanceAfterSetup()
    {
        Should.Throw<ArgumentNullException>(() => AppBuilder.Configure<TestApplication>().UseI18Next(null));
        AppBuilder.Configure<TestApplication>().UseI18Next(_i18Next).ShouldNotBeNull();
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
