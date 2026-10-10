using System;

using Microsoft.EntityFrameworkCore;

namespace I18Next.Net.EntityFrameworkCore;

public static class ModelBuilderExtensions
{
    /// <summary>
    ///     Adds the <see cref="TranslationEntry" /> entity storing the translations to the model of a context. Call it in
    ///     <c>OnModelCreating</c> of the context used by the <see cref="EntityFrameworkBackend{TContext}" />.
    /// </summary>
    /// <param name="modelBuilder">The model builder of the context.</param>
    /// <param name="tableName">The name of the table storing the translations.</param>
    /// <param name="schema">The schema of the table or <c>null</c> for the default schema.</param>
    /// <returns>The model builder.</returns>
    public static ModelBuilder ApplyI18NextTranslations(this ModelBuilder modelBuilder, string tableName = TranslationEntryConfiguration.DefaultTableName,
        string schema = null)
    {
        if (modelBuilder == null)
            throw new ArgumentNullException(nameof(modelBuilder));

        return modelBuilder.ApplyConfiguration(new TranslationEntryConfiguration(tableName, schema));
    }
}
