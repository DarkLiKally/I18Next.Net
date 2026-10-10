using Microsoft.EntityFrameworkCore;

namespace I18Next.Net.EntityFrameworkCore.Tests;

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyI18NextTranslations();
    }
}
