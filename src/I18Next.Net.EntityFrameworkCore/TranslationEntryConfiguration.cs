using System;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace I18Next.Net.EntityFrameworkCore;

/// <summary>
///     Configures the <see cref="TranslationEntry" /> entity with a unique index on the language, namespace and key.
/// </summary>
public class TranslationEntryConfiguration : IEntityTypeConfiguration<TranslationEntry>
{
    /// <summary>
    ///     The default name of the table storing the translations.
    /// </summary>
    public const string DefaultTableName = "I18NextTranslations";

    private readonly string _schema;
    private readonly string _tableName;

    /// <summary>
    ///     Creates a configuration mapping the entity to the given table.
    /// </summary>
    /// <param name="tableName">The name of the table storing the translations.</param>
    /// <param name="schema">The schema of the table or <c>null</c> for the default schema.</param>
    /// <exception cref="ArgumentException">If <paramref name="tableName" /> is <c>null</c>, empty or whitespace.</exception>
    public TranslationEntryConfiguration(string tableName = DefaultTableName, string schema = null)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null, empty or whitespace string.", nameof(tableName));

        _tableName = tableName;
        _schema = schema;
    }

    /// <summary>
    ///     Maps the entity to the table, limits the lengths of the language, namespace and key columns and adds the unique
    ///     index.
    /// </summary>
    /// <param name="builder">The builder of the entity.</param>
    public void Configure(EntityTypeBuilder<TranslationEntry> builder)
    {
        builder.ToTable(_tableName, _schema);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Language).HasMaxLength(TranslationEntry.LanguageMaxLength).IsRequired();
        builder.Property(e => e.Namespace).HasMaxLength(TranslationEntry.NamespaceMaxLength).IsRequired();
        builder.Property(e => e.Key).HasMaxLength(TranslationEntry.KeyMaxLength).IsRequired();
        builder.HasIndex(e => new { e.Language, e.Namespace, e.Key }).IsUnique();
    }
}
