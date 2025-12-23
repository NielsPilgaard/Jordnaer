using Jordnaer.Database;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Features.Profile;

/// <summary>
/// One-time migration service that fetches location data from DataForsyningen for existing users with zip codes.
/// Converts ZipCode + City to Location Point geometry by calling the external API.
/// Runs once on application startup and marks completion to avoid re-running.
/// </summary>
public class LocationMigrationService(
	IServiceScopeFactory serviceScopeFactory,
	ILogger<LocationMigrationService> logger) : BackgroundService
{
	private const int BatchSize = 50; // Process in batches to avoid overloading DataForsyningen API
	private const int DelayBetweenBatchesMs = 1000; // 1 second delay between batches

	private ILocationService locationService = null!;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			await using var scope = serviceScopeFactory.CreateAsyncScope();

			var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JordnaerDbContext>>();
			locationService = scope.ServiceProvider.GetRequiredService<ILocationService>();
			await using var context = await contextFactory.CreateDbContextAsync(stoppingToken);

			logger.LogInformation("Starting location migration from ZipCode to Point geometry");

			// Migrate UserProfiles
			var userProfilesUpdated = await MigrateUserProfiles(context, stoppingToken);
			logger.LogInformation("Migrated {Count} user profiles", userProfilesUpdated);

			// Migrate Groups
			var groupsUpdated = await MigrateGroups(context, stoppingToken);
			logger.LogInformation("Migrated {Count} groups", groupsUpdated);

			// Migrate Posts
			var postsUpdated = await MigratePosts(context, stoppingToken);
			logger.LogInformation("Migrated {Count} posts", postsUpdated);

			logger.LogInformation(
				"Location migration completed successfully. Total: {UserProfiles} users, {Groups} groups, {Posts} posts",
				userProfilesUpdated, groupsUpdated, postsUpdated);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Location migration failed. This is non-critical and the application will continue running.");
		}
	}

	private async Task<int> MigrateUserProfiles(JordnaerDbContext context, CancellationToken cancellationToken)
	{
		try
		{
			// Get all users with ZipCode and City but no Location
			var usersToMigrate = await context.UserProfiles
				.Where(u => u.ZipCode != null && u.City != null && u.Location == null)
				.Select(u => new { u.Id, u.ZipCode, u.City })
				.ToListAsync(cancellationToken);

			if (usersToMigrate.Count == 0)
			{
				logger.LogInformation("No user profiles need migration");
				return 0;
			}

			logger.LogInformation("Found {Count} user profiles to migrate", usersToMigrate.Count);

			var successCount = 0;
			var batches = usersToMigrate.Chunk(BatchSize);

			foreach (var batch in batches)
			{
				foreach (var user in batch)
				{
					if (cancellationToken.IsCancellationRequested)
						break;

					try
					{
						var zipCodeText = $"{user.ZipCode} {user.City}";
						var locationResult = await locationService.GetLocationFromZipCodeAsync(zipCodeText, cancellationToken);

						if (locationResult != null)
						{
							await context.Database.ExecuteSqlRawAsync(
								"UPDATE UserProfiles SET Location = {0} WHERE Id = {1}",
								locationResult.Location,
								user.Id);

							successCount++;
						}
						else
						{
							logger.LogWarning("Failed to get location for user {UserId} with ZipCode {ZipCode}", user.Id, zipCodeText);
						}
					}
					catch (Exception ex)
					{
						logger.LogWarning(ex, "Error migrating user {UserId}", user.Id);
					}
				}

				// Delay between batches to avoid rate limiting
				if (cancellationToken.IsCancellationRequested)
					break;

				await Task.Delay(DelayBetweenBatchesMs, cancellationToken);
			}

			return successCount;
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to migrate user profiles");
			return 0;
		}
	}

	private async Task<int> MigrateGroups(JordnaerDbContext context, CancellationToken cancellationToken)
	{
		try
		{
			var groupsToMigrate = await context.Groups
				.Where(g => g.ZipCode != null && g.City != null && g.Location == null)
				.Select(g => new { g.Id, g.ZipCode, g.City })
				.ToListAsync(cancellationToken);

			if (groupsToMigrate.Count == 0)
			{
				logger.LogInformation("No groups need migration");
				return 0;
			}

			logger.LogInformation("Found {Count} groups to migrate", groupsToMigrate.Count);

			var successCount = 0;
			var batches = groupsToMigrate.Chunk(BatchSize);

			foreach (var batch in batches)
			{
				foreach (var group in batch)
				{
					if (cancellationToken.IsCancellationRequested)
						break;

					try
					{
						var zipCodeText = $"{group.ZipCode} {group.City}";
						var locationResult = await locationService.GetLocationFromZipCodeAsync(zipCodeText, cancellationToken);

						if (locationResult != null)
						{
							await context.Database.ExecuteSqlRawAsync(
								"UPDATE Groups SET Location = {0} WHERE Id = {1}",
								locationResult.Location,
								group.Id);

							successCount++;
						}
						else
						{
							logger.LogWarning("Failed to get location for group {GroupId} with ZipCode {ZipCode}", group.Id, zipCodeText);
						}
					}
					catch (Exception ex)
					{
						logger.LogWarning(ex, "Error migrating group {GroupId}", group.Id);
					}
				}

				if (cancellationToken.IsCancellationRequested)
					break;

				await Task.Delay(DelayBetweenBatchesMs, cancellationToken);
			}

			return successCount;
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to migrate groups");
			return 0;
		}
	}

	private async Task<int> MigratePosts(JordnaerDbContext context, CancellationToken cancellationToken)
	{
		try
		{
			var postsToMigrate = await context.Posts
				.Where(p => p.ZipCode != null && p.Location == null)
				.Select(p => new { p.Id, p.ZipCode })
				.ToListAsync(cancellationToken);

			if (postsToMigrate.Count == 0)
			{
				logger.LogInformation("No posts need migration");
				return 0;
			}

			logger.LogInformation("Found {Count} posts to migrate", postsToMigrate.Count);

			var successCount = 0;
			var batches = postsToMigrate.Chunk(BatchSize);

			foreach (var batch in batches)
			{
				foreach (var post in batch)
				{
					if (cancellationToken.IsCancellationRequested)
						break;

					try
					{
						var zipCodeText = post.ZipCode.ToString()!;
						var locationResult = await locationService.GetLocationFromZipCodeAsync(zipCodeText, cancellationToken);

						if (locationResult != null)
						{
							await context.Database.ExecuteSqlRawAsync(
								"UPDATE Posts SET Location = {0} WHERE Id = {1}",
								locationResult.Location,
								post.Id);

							successCount++;
						}
						else
						{
							logger.LogWarning("Failed to get location for post {PostId} with ZipCode {ZipCode}", post.Id, zipCodeText);
						}
					}
					catch (Exception ex)
					{
						logger.LogWarning(ex, "Error migrating post {PostId}", post.Id);
					}
				}

				if (cancellationToken.IsCancellationRequested)
					break;

				await Task.Delay(DelayBetweenBatchesMs, cancellationToken);
			}

			return successCount;
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to migrate posts");
			return 0;
		}
	}
}
