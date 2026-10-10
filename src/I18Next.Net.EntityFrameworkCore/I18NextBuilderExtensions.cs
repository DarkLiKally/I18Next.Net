using System;

using I18Next.Net.Extensions.Builder;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace I18Next.Net.EntityFrameworkCore;

public static class I18NextBuilderExtensions
{
    /// <summary>
    ///     Registers an <see cref="EntityFrameworkBackend{TContext}" /> using the <see cref="IDbContextFactory{TContext}" />
    ///     registered with <c>AddDbContextFactory</c>. The backend is also registered as
    ///     <see cref="EntityFrameworkBackend{TContext}" />, so translations can be changed through it at runtime.
    /// </summary>
    /// <param name="builder">The I18Next builder.</param>
    /// <param name="configureBackend">Configures the backend instance.</param>
    /// <typeparam name="TContext">The type of the context.</typeparam>
    /// <returns>The current I18Next builder instance.</returns>
    public static I18NextBuilder AddEntityFrameworkBackend<TContext>(this I18NextBuilder builder,
        Action<EntityFrameworkBackend<TContext>> configureBackend = null)
        where TContext : DbContext
    {
        builder.Services.AddSingleton(c =>
        {
            var backend = new EntityFrameworkBackend<TContext>(c.GetRequiredService<IDbContextFactory<TContext>>());

            configureBackend?.Invoke(backend);

            return backend;
        });

        return builder.AddBackend(c => c.GetRequiredService<EntityFrameworkBackend<TContext>>());
    }

    /// <summary>
    ///     Registers an <see cref="EntityFrameworkMissingKeyHandler{TContext}" /> adding missing keys to the database using the
    ///     <see cref="IDbContextFactory{TContext}" /> registered with <c>AddDbContextFactory</c>.
    /// </summary>
    /// <param name="builder">The I18Next builder.</param>
    /// <param name="language">
    ///     The language missing keys are added for, e.g. the development language. <c>null</c> adds them for the language
    ///     they are missing in.
    /// </param>
    /// <typeparam name="TContext">The type of the context.</typeparam>
    /// <returns>The current I18Next builder instance.</returns>
    public static I18NextBuilder AddEntityFrameworkMissingKeyHandler<TContext>(this I18NextBuilder builder, string language = null)
        where TContext : DbContext
    {
        return builder.AddMissingKeyHandler(c =>
            new EntityFrameworkMissingKeyHandler<TContext>(c.GetRequiredService<IDbContextFactory<TContext>>()) { Language = language });
    }
}
