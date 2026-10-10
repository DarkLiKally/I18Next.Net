using System;
using System.Linq;
using System.Threading;

using I18Next.Net.Backends;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Hosting;

using Shouldly;

using Xunit;

namespace I18Next.Net.Maui.Tests;

[Collection(nameof(I18NextXaml))]
public class MauiAppBuilderExtensionsFixture : IDisposable
{
    public MauiAppBuilderExtensionsFixture()
    {
        TestDispatcher.Use();
        SynchronizationContext.SetSynchronizationContext(null);
    }

    public void Dispose()
    {
        I18NextXaml.Instance = null;
    }

    [Fact]
    public void UseI18Next_ShouldRegisterI18NextAndUseItInXaml()
    {
        var backend = new InMemoryBackend();
        backend.AddTranslation("de", "translation", "menu.title", "Menü");

        var builder = MauiApp.CreateBuilder();
        builder.UseI18Next(i18n => i18n.AddBackend(backend).UseDefaultLanguage("de"));

        using var app = builder.Build();

        I18NextXaml.Instance.ShouldBeSameAs(app.Services.GetRequiredService<II18Next>());

        var label = new Label().LoadFromXaml(
            """<Label xmlns="http://schemas.microsoft.com/dotnet/2021/maui" xmlns:i18n="https://github.com/DarkLiKally/I18Next.Net" Text="{i18n:T menu.title}" />""");

        label.Text.ShouldBe("Menü");
    }

    [Fact]
    public void UseI18Next_Defaults_ShouldRegisterInitializerOnce()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseI18Next();
        builder.UseI18Next(null);

        builder.Services.Count(s => s.ServiceType == typeof(IMauiInitializeService) && s.ImplementationType?.DeclaringType == typeof(MauiAppBuilderExtensions))
            .ShouldBe(1);

        using var app = builder.Build();

        I18NextXaml.Instance.ShouldNotBeNull();
        I18NextXaml.Instance.Language.ShouldBe("en-US");
    }
}
