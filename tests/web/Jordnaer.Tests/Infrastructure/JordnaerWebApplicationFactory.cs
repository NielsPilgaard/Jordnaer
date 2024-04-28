using Jordnaer.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Testcontainers.Azurite;
using Testcontainers.MsSql;
using Xunit;

namespace Jordnaer.Tests.Infrastructure;

public class JordnaerWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
	private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
		.WithName($"SqlServerTestcontainer-{Guid.NewGuid()}")
		.Build();

	private readonly AzuriteContainer _azureBlobStorageContainer = new AzuriteBuilder()
		.WithName($"AzuriteTestcontainer-{Guid.NewGuid()}")
		.WithInMemoryPersistence()
		.Build();

	public async Task InitializeAsync()
	{
		await _msSqlContainer.StartAsync();
		await _azureBlobStorageContainer.StartAsync();
	}

	public new async Task DisposeAsync()
	{
		await _msSqlContainer.DisposeAsync();
		await _azureBlobStorageContainer.DisposeAsync();
		await base.DisposeAsync();
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseSetting($"ConnectionStrings:{nameof(JordnaerDbContext)}", _msSqlContainer.GetConnectionString());

		builder.UseSetting("ConnectionStrings:AzureBlobStorage", _azureBlobStorageContainer.GetConnectionString());

		builder.ConfigureTestServices(services => services.RemoveAll<IHostedService>());

		builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
	}
}
