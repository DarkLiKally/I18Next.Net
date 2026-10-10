using System;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using Shouldly;

using Xunit;

namespace I18Next.Net.EntityFrameworkCore.Tests;

public class ModelBuilderExtensionsFixture
{
    [Fact]
    public void ApplyI18NextTranslations_Defaults_ShouldConfigureEntity()
    {
        using var database = new TestDatabase();
        using var context = database.ContextFactory.CreateDbContext();

        var entityType = context.Model.FindEntityType(typeof(TranslationEntry)).ShouldNotBeNull();

        entityType.GetTableName().ShouldBe(TranslationEntryConfiguration.DefaultTableName);
        entityType.GetSchema().ShouldBeNull();
        entityType.FindPrimaryKey().ShouldNotBeNull().Properties.ShouldHaveSingleItem().Name.ShouldBe(nameof(TranslationEntry.Id));
        entityType.FindProperty(nameof(TranslationEntry.Language)).ShouldNotBeNull().GetMaxLength().ShouldBe(TranslationEntry.LanguageMaxLength);
        entityType.FindProperty(nameof(TranslationEntry.Namespace)).ShouldNotBeNull().GetMaxLength().ShouldBe(TranslationEntry.NamespaceMaxLength);
        entityType.FindProperty(nameof(TranslationEntry.Key)).ShouldNotBeNull().GetMaxLength().ShouldBe(TranslationEntry.KeyMaxLength);
        entityType.FindProperty(nameof(TranslationEntry.Language)).IsNullable.ShouldBeFalse();
        entityType.FindProperty(nameof(TranslationEntry.Namespace)).IsNullable.ShouldBeFalse();
        entityType.FindProperty(nameof(TranslationEntry.Key)).IsNullable.ShouldBeFalse();
        entityType.FindProperty(nameof(TranslationEntry.Value)).ShouldNotBeNull().IsNullable.ShouldBeTrue();

        var index = entityType.GetIndexes().ShouldHaveSingleItem();
        index.IsUnique.ShouldBeTrue();
        index.Properties.ShouldAllBe(p => p.Name == nameof(TranslationEntry.Language) || p.Name == nameof(TranslationEntry.Namespace) ||
                                          p.Name == nameof(TranslationEntry.Key));
        index.Properties.Count.ShouldBe(3);
    }

    [Fact]
    public void ApplyI18NextTranslations_TableNameAndSchema_ShouldBeUsed()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        using var context = new CustomTableContext(new DbContextOptionsBuilder<CustomTableContext>().UseSqlite(connection).Options);

        var entityType = context.Model.FindEntityType(typeof(TranslationEntry)).ShouldNotBeNull();

        entityType.GetTableName().ShouldBe("Texts");
        entityType.GetSchema().ShouldBe("i18n");
    }

    [Fact]
    public void InvalidArguments_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => ((ModelBuilder)null).ApplyI18NextTranslations());
        Should.Throw<ArgumentException>(() => new TranslationEntryConfiguration(" "));
        Should.Throw<ArgumentException>(() => new TranslationEntryConfiguration(null));
    }

    private class CustomTableContext(DbContextOptions<CustomTableContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyI18NextTranslations("Texts", "i18n");
        }
    }
}
