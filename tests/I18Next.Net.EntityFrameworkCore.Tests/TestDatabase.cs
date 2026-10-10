using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace I18Next.Net.EntityFrameworkCore.Tests;

public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestDatabase(Action<IServiceCollection> configureServices = null, Func<DbContext, Task> onSavingChanges = null)
    {
        _connection.Open();

        var services = new ServiceCollection();
        services.AddDbContextFactory<TestDbContext>(options =>
        {
            options.UseSqlite(_connection);

            if (onSavingChanges != null)
                options.AddInterceptors(new SavingChangesInterceptor(onSavingChanges));
        });
        configureServices?.Invoke(services);

        Services = services.BuildServiceProvider();
        ContextFactory = Services.GetRequiredService<IDbContextFactory<TestDbContext>>();

        using var context = ContextFactory.CreateDbContext();
        context.Database.EnsureCreated();
    }

    public IDbContextFactory<TestDbContext> ContextFactory { get; }

    public ServiceProvider Services { get; }

    public void Dispose()
    {
        Services.Dispose();
        _connection.Dispose();
    }

    public async Task AddAsync(string language, string @namespace, string key, string value)
    {
        using var context = ContextFactory.CreateDbContext();

        context.Add(new TranslationEntry { Language = language, Namespace = @namespace, Key = key, Value = value });

        await context.SaveChangesAsync();
    }

    public List<TranslationEntry> GetEntries()
    {
        using var context = ContextFactory.CreateDbContext();

        return context.Set<TranslationEntry>().AsNoTracking().OrderBy(e => e.Id).ToList();
    }

    private sealed class SavingChangesInterceptor(Func<DbContext, Task> onSavingChanges) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await onSavingChanges(eventData.Context);

            return result;
        }
    }
}
