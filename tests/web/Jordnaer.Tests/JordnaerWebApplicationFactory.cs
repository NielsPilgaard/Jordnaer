using Jordnaer.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Testcontainers.MsSql;
using Xunit;

namespace Jordnaer.Tests;

public class JordnaerWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
	public readonly MsSqlContainer Container = new MsSqlBuilder()
		.WithName($"SqlServerTestcontainer-{Guid.NewGuid()}")
		.Build();

	public async Task InitializeAsync() => await Container.StartAsync();

	public new async Task DisposeAsync()
	{
		await Container.DisposeAsync();
		await base.DisposeAsync();
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.ConfigureTestServices(services =>
		{
			services.RemoveAll<IHostedService>();

			services.RemoveAll<JordnaerDbContext>();
			services.RemoveAll<DbContextOptions<JordnaerDbContext>>();

			services.AddSqlServer<JordnaerDbContext>(Container.GetConnectionString());
		});

		builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
	}
}
