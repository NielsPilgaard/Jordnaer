using Jordnaer.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Testcontainers.MsSql;
using Xunit;

namespace Jordnaer.Tests.Infrastructure;

public class JordnaerWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
	public readonly MsSqlContainer MsSqlContainer = new MsSqlBuilder()
		.WithName($"SqlServerTestcontainer-{Guid.NewGuid()}")
		.Build();

	public async Task InitializeAsync() => await MsSqlContainer.StartAsync();

	public new async Task DisposeAsync()
	{
		await MsSqlContainer.DisposeAsync();
		await base.DisposeAsync();
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseSetting($"ConnectionStrings:{nameof(JordnaerDbContext)}", MsSqlContainer.GetConnectionString());

		builder.ConfigureTestServices(services =>
		{
			services.RemoveAll<IHostedService>();
		});

		builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
	}
}
