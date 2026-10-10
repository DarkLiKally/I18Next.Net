using System;
using System.Linq;
using System.Threading.Tasks;

using I18Next.Net.Plugins;

using Microsoft.EntityFrameworkCore;

using Shouldly;

using Xunit;

namespace I18Next.Net.EntityFrameworkCore.Tests;

public class EntityFrameworkMissingKeyHandlerFixture
{
    [Fact]
    public async Task HandleMissingKeyAsync_ShouldAddKeyWithoutValueOnce()
    {
        using var database = new TestDatabase();
        var handler = new EntityFrameworkMissingKeyHandler<TestDbContext>(database.ContextFactory);

        await handler.HandleMissingKeyAsync(this, CreateArgs("de-AT", "translation", "greeting"));
        await handler.HandleMissingKeyAsync(this, CreateArgs("de-AT", "translation", "greeting"));
        await new EntityFrameworkMissingKeyHandler<TestDbContext>(database.ContextFactory).HandleMissingKeyAsync(this,
            CreateArgs("de-AT", "translation", "greeting"));

        var entry = database.GetEntries().ShouldHaveSingleItem();
        entry.Language.ShouldBe("de-AT");
        entry.Namespace.ShouldBe("translation");
        entry.Key.ShouldBe("greeting");
        entry.Value.ShouldBeNull();
    }

    [Fact]
    public async Task HandleMissingKeyAsync_Language_ShouldAddKeyForLanguage()
    {
        using var database = new TestDatabase();
        var handler = new EntityFrameworkMissingKeyHandler<TestDbContext>(database.ContextFactory) { Language = "en" };

        await handler.HandleMissingKeyAsync(this, CreateArgs("de", "translation", "greeting"));
        await handler.HandleMissingKeyAsync(this, CreateArgs("fr", "translation", "greeting"));
        await handler.HandleMissingKeyAsync(this, CreateArgs("fr", "common", "greeting"));

        database.GetEntries().Select(e => (e.Language, e.Namespace, e.Key)).ShouldBe([("en", "translation", "greeting"), ("en", "common", "greeting")]);
    }

    [Fact]
    public async Task HandleMissingKeyAsync_ExistingTranslation_ShouldNotChangeIt()
    {
        using var database = new TestDatabase();
        await database.AddAsync("en", "translation", "greeting", "Hello");
        var handler = new EntityFrameworkMissingKeyHandler<TestDbContext>(database.ContextFactory);

        await handler.HandleMissingKeyAsync(this, CreateArgs("en", "translation", "greeting"));

        database.GetEntries().ShouldHaveSingleItem().Value.ShouldBe("Hello");
    }

    [Theory]
    [InlineData("", "translation", "key")]
    [InlineData("en", " ", "key")]
    [InlineData("en", "translation", null)]
    [InlineData("en-0123456789012345678901234567890123", "translation", "key")]
    public async Task HandleMissingKeyAsync_InvalidOrTooLong_ShouldBeIgnored(string language, string ns, string key)
    {
        using var database = new TestDatabase();
        var handler = new EntityFrameworkMissingKeyHandler<TestDbContext>(database.ContextFactory);

        await handler.HandleMissingKeyAsync(this, CreateArgs(language, ns, key));
        await handler.HandleMissingKeyAsync(this, CreateArgs("en", new string('n', TranslationEntry.NamespaceMaxLength + 1), "key"));
        await handler.HandleMissingKeyAsync(this, CreateArgs("en", "translation", new string('k', TranslationEntry.KeyMaxLength + 1)));

        database.GetEntries().ShouldBeEmpty();

        await handler.HandleMissingKeyAsync(this, CreateArgs("en", "translation", new string('k', TranslationEntry.KeyMaxLength)));

        database.GetEntries().ShouldHaveSingleItem();
    }

    [Fact]
    public async Task HandleMissingKeyAsync_AddedConcurrently_ShouldIgnoreDuplicate()
    {
        using var database = new TestDatabase(onSavingChanges: context => context.Database.ExecuteSqlRawAsync(
            "INSERT INTO I18NextTranslations (Language, Namespace, Key, Value) VALUES ('en', 'translation', 'greeting', 'Hello')"));
        var handler = new EntityFrameworkMissingKeyHandler<TestDbContext>(database.ContextFactory);

        await handler.HandleMissingKeyAsync(this, CreateArgs("en", "translation", "greeting"));

        database.GetEntries().ShouldHaveSingleItem().Value.ShouldBe("Hello");
    }

    [Fact]
    public async Task HandleMissingKeyAsync_SaveFails_ShouldThrowAndRetryLater()
    {
        var fail = true;
        using var database = new TestDatabase(onSavingChanges: context =>
        {
            if (fail)
            {
                foreach (var entry in context.ChangeTracker.Entries<TranslationEntry>())
                    entry.Entity.Language = null;
            }

            return Task.CompletedTask;
        });
        var handler = new EntityFrameworkMissingKeyHandler<TestDbContext>(database.ContextFactory);

        await Should.ThrowAsync<DbUpdateException>(() => handler.HandleMissingKeyAsync(this, CreateArgs("en", "translation", "greeting")));

        database.GetEntries().ShouldBeEmpty();
        fail = false;

        await handler.HandleMissingKeyAsync(this, CreateArgs("en", "translation", "greeting"));

        database.GetEntries().ShouldHaveSingleItem().Key.ShouldBe("greeting");
    }

    [Fact]
    public async Task InvalidArguments_ShouldThrow()
    {
        using var database = new TestDatabase();

        Should.Throw<ArgumentNullException>(() => new EntityFrameworkMissingKeyHandler<TestDbContext>(null));
        await Should.ThrowAsync<ArgumentNullException>(() =>
            new EntityFrameworkMissingKeyHandler<TestDbContext>(database.ContextFactory).HandleMissingKeyAsync(this, null));
    }

    [Fact]
    public async Task Translator_MissingKey_ShouldBeAddedAndTranslatedAfterSettingValue()
    {
        using var database = new TestDatabase();
        await database.AddAsync("en", "translation", "greeting", "Hello");
        var backend = new EntityFrameworkBackend<TestDbContext>(database.ContextFactory);
        var translator = new DefaultTranslator(backend);
        translator.MissingKeyHandlers.Add(new EntityFrameworkMissingKeyHandler<TestDbContext>(database.ContextFactory));
        var i18Next = new I18NextNet(backend, translator) { Language = "en" };

        (await i18Next.Ta("farewell")).ShouldBe("farewell");
        (await i18Next.Ta("farewell")).ShouldBe("farewell");

        database.GetEntries().Select(e => (e.Key, e.Value)).ShouldBe([("greeting", "Hello"), ("farewell", null)]);

        await backend.SetValueAsync("en", "translation", "farewell", "Goodbye");

        (await i18Next.Ta("farewell")).ShouldBe("Goodbye");
        database.GetEntries().Count.ShouldBe(2);
    }

    private static MissingKeyEventArgs CreateArgs(string language, string ns, string key)
    {
        return new MissingKeyEventArgs(language, ns, key, [key]);
    }
}
