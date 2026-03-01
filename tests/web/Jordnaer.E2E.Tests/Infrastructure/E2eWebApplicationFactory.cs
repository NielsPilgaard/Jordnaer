using Jordnaer.Database;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Testcontainers.Azurite;
using Testcontainers.MsSql;

namespace Jordnaer.E2E.Tests.Infrastructure;

public class E2eWebApplicationFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
	/// <summary>
	/// The actual base URL the Kestrel server is listening on (e.g. http://127.0.0.1:5123).
	/// Available after <see cref="InitializeAsync"/> completes.
	/// </summary>
	public string ServerAddress =>
		_kestrelHost!.Services
			.GetRequiredService<IServer>()
			.Features.Get<IServerAddressesFeature>()!
			.Addresses.First()
			.TrimEnd('/');

	public const string UserAEmail = "user-a@e2e.test";
	public const string UserAPassword = "E2eUserA!1";
	public const string UserBEmail = "user-b@e2e.test";
	public const string UserBPassword = "E2eUserB!1";

	private IHost? _kestrelHost;

	private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
		.WithName($"SqlServerE2E-{Guid.NewGuid()}")
		.Build();

	private readonly AzuriteContainer _azureBlobStorageContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
		.WithName($"AzuriteE2E-{Guid.NewGuid()}")
		.WithInMemoryPersistence()
		.WithCommand("--skipApiVersionCheck")
		.Build();

	public async Task InitializeAsync()
	{
		await _msSqlContainer.StartAsync();
		await _azureBlobStorageContainer.StartAsync();

		// Boot the app — this triggers CreateHost which starts both the TestServer
		// and the real Kestrel host.
		_ = Services;

		await SeedUsersAsync();
	}

	public override async ValueTask DisposeAsync()
	{
		if (_kestrelHost is not null)
		{
			await _kestrelHost.StopAsync();
			_kestrelHost.Dispose();
		}

		await _msSqlContainer.DisposeAsync();
		await _azureBlobStorageContainer.DisposeAsync();
		await base.DisposeAsync();
	}

	protected override IHost CreateHost(IHostBuilder builder)
	{
		// Build the standard TestServer host that WebApplicationFactory expects.
		// This keeps all WebApplicationFactory internals (Services, Server cast, etc.) happy.
		var testServerHost = base.CreateHost(builder);

		// Now build a second host with a real Kestrel server on a random loopback port
		// so that Playwright (an out-of-process browser) can connect via a real TCP socket.
		builder.ConfigureWebHost(webHostBuilder =>
			webHostBuilder.UseKestrel(o => o.Listen(System.Net.IPAddress.Loopback, 0)));

		_kestrelHost = builder.Build();
		_kestrelHost.Start();

		return testServerHost;
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseSetting($"ConnectionStrings:{nameof(JordnaerDbContext)}", _msSqlContainer.GetConnectionString());

		builder.UseSetting("ConnectionStrings:AzureBlobStorage", _azureBlobStorageContainer.GetConnectionString());

		// Fake key - required to satisfy DI registration, but no emails are sent in tests
		builder.UseSetting("ConnectionStrings:AzureEmailService", "endpoint=https://jordnaer.europe.communication.azure.com/;accesskey=REDACTED");

		builder.ConfigureTestServices(services => services.RemoveAll<IHostedService>());

		builder.ConfigureLogging(loggingBuilder =>
		{
			loggingBuilder.ClearProviders();
			loggingBuilder.SetMinimumLevel(LogLevel.Error);
			loggingBuilder.AddConsole();
		});
	}

	private async Task SeedUsersAsync()
	{
		// Use the Kestrel host's services so we hit the same DB the real server uses.
		await using var scope = _kestrelHost!.Services.CreateAsyncScope();
		var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JordnaerDbContext>>();
		await using var context = await dbContextFactory.CreateDbContextAsync();

		await context.Database.MigrateAsync();

		var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

		await CreateTestUserAsync(userManager, context, UserAEmail, UserAPassword, "User", "A");
		await CreateTestUserAsync(userManager, context, UserBEmail, UserBPassword, "User", "B");
	}

	private static async Task CreateTestUserAsync(
		UserManager<ApplicationUser> userManager,
		JordnaerDbContext context,
		string email,
		string password,
		string firstName,
		string lastName)
	{
		var existing = await userManager.FindByEmailAsync(email);
		if (existing is not null)
		{
			// User exists — ensure the matching UserProfile also exists
			var existingProfile = await context.UserProfiles
				.AsNoTracking()
				.FirstOrDefaultAsync(p => p.Id == existing.Id);

			if (existingProfile is null)
			{
				context.UserProfiles.Add(new UserProfile
				{
					Id = existing.Id,
					UserName = email,
					FirstName = firstName,
					LastName = lastName
				});
				await context.SaveChangesAsync();
			}

			return;
		}

		var user = new ApplicationUser
		{
			UserName = email,
			Email = email,
			EmailConfirmed = true
		};

		var result = await userManager.CreateAsync(user, password);
		if (!result.Succeeded)
		{
			throw new InvalidOperationException(
				$"Failed to create test user '{email}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
		}

		// Create a matching UserProfile so the user appears in the app properly
		context.UserProfiles.Add(new UserProfile
		{
			Id = user.Id,
			UserName = email,
			FirstName = firstName,
			LastName = lastName
		});
		await context.SaveChangesAsync();
	}
}
