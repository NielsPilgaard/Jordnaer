using Jordnaer.Database;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Hosting;
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
	public const string UserAEmail = "user-a@e2e.test";
	public const string UserAPassword = "E2eUserA!1";
	public const string UserBEmail = "user-b@e2e.test";
	public const string UserBPassword = "E2eUserB!1";

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

		// Boot the app so migrations run and UserManager is available
		_ = Server;

		await SeedUsersAsync();
	}

	public new async ValueTask DisposeAsync()
	{
		await _msSqlContainer.DisposeAsync();
		await _azureBlobStorageContainer.DisposeAsync();
		await base.DisposeAsync();
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseSetting($"ConnectionStrings:{nameof(JordnaerDbContext)}", _msSqlContainer.GetConnectionString());

		builder.UseSetting("ConnectionStrings:AzureBlobStorage", _azureBlobStorageContainer.GetConnectionString());

		// Fake key - required to satisfy DI registration, but no emails are sent in tests
		builder.UseSetting("ConnectionStrings:AzureEmailService", "endpoint=https://jordnaer.europe.communication.azure.com/;accesskey=GHrGMddff66e6oVOgjxEytm5B5fwwpJCwRJ223ACUL425AAffdvvvcc32lI");

		builder.ConfigureTestServices(services => services.RemoveAll<IHostedService>());

		builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
	}

	private async Task SeedUsersAsync()
	{
		await using var scope = Services.CreateAsyncScope();
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
		var profile = new UserProfile
		{
			Id = user.Id,
			UserName = email,
			FirstName = firstName,
			LastName = lastName
		};

		context.UserProfiles.Add(profile);
		await context.SaveChangesAsync();
	}
}
