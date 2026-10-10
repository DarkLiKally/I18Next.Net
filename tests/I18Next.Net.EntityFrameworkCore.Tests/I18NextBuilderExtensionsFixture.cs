using System;
using System.Linq;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Extensions;
using I18Next.Net.Plugins;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

namespace I18Next.Net.EntityFrameworkCore.Tests;

public class I18NextBuilderExtensionsFixture
{
    [Fact]
    public async Task AddEntityFrameworkBackend_ShouldRegisterSharedBackend()
    {
        using var database = new TestDatabase(services => services.AddI18NextLocalization(i18n => i18n
            .AddEntityFrameworkBackend<TestDbContext>(backend => backend.CacheExpiration = TimeSpan.FromMinutes(5))
            .UseDefaultLanguage("de-AT")));
        await database.AddAsync("de", "translation", "greeting", "Hallo");

        var backend = database.Services.GetRequiredService<EntityFrameworkBackend<TestDbContext>>();

        database.Services.GetRequiredService<ITranslationBackend>().ShouldBeSameAs(backend);
        backend.CacheExpiration.ShouldBe(TimeSpan.FromMinutes(5));

        var i18Next = database.Services.GetRequiredService<II18Next>();
        i18Next.T("greeting").ShouldBe("Hallo");

        await backend.SetValueAsync("de", "translation", "greeting", "Servus");

        i18Next.T("greeting").ShouldBe("Servus");
    }

    [Fact]
    public async Task AddEntityFrameworkMissingKeyHandler_ShouldAddMissingKeys()
    {
        using var database = new TestDatabase(services => services.AddI18NextLocalization(i18n => i18n
            .AddEntityFrameworkBackend<TestDbContext>()
            .AddEntityFrameworkMissingKeyHandler<TestDbContext>("en")
            .UseDefaultLanguage("de")));
        await database.AddAsync("de", "translation", "greeting", "Hallo");

        database.Services.GetRequiredService<IMissingKeyHandler>().ShouldBeOfType<EntityFrameworkMissingKeyHandler<TestDbContext>>().Language.ShouldBe("en");

        database.Services.GetRequiredService<II18Next>().T("farewell").ShouldBe("farewell");

        database.GetEntries().Select(e => (e.Language, e.Key, e.Value)).ShouldBe([("de", "greeting", "Hallo"), ("en", "farewell", null)]);
    }
}
