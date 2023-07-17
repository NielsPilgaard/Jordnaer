using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace Jordnaer.Server.Tests;

public class SqlServerContainer<TDbContext> : IAsyncLifetime where TDbContext : DbContext
{
    public readonly MsSqlContainer Container = new MsSqlBuilder()
        .WithName($"SqlServerTestcontainer-{Guid.NewGuid()}")
        .Build();

    public TDbContext Context = null!;

    public virtual async Task InitializeAsync()
    {
        await Container.StartAsync();

        string? connectionString = Container.GetConnectionString();

        Context = (TDbContext)Activator
            .CreateInstance(typeof(TDbContext),
                new DbContextOptionsBuilder<TDbContext>().UseSqlServer(connectionString).Options)!;

        await Context!.Database.EnsureCreatedAsync();
    }

    public virtual async Task DisposeAsync() => await Container.StopAsync();
}
