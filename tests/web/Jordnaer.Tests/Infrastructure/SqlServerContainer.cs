using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace Jordnaer.Tests.Infrastructure;

public class SqlServerContainer<TDbContext> : IAsyncLifetime where TDbContext : DbContext
{
	public readonly MsSqlContainer Container = new MsSqlBuilder()
		.WithName($"SqlServerTestcontainer-{Guid.NewGuid()}")
		.Build();

	public TDbContext CreateContext() => (TDbContext)Activator
		.CreateInstance(typeof(TDbContext),
						new DbContextOptionsBuilder<TDbContext>()
							.UseSqlServer(_connectionString)
							.Options)!;

	private string _connectionString = null!;

	public virtual async Task InitializeAsync()
	{
		await Container.StartAsync();

		_connectionString = Container.GetConnectionString();

		await using var context = CreateContext();
		await context.Database.EnsureCreatedAsync();
	}

	public virtual async Task DisposeAsync() => await Container.DisposeAsync();
}
