using Jordnaer.Database;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Features.Notifications;

public class NotificationCleanupService(
	IDbContextFactory<JordnaerDbContext> dbContextFactory,
	IConfiguration configuration,
	ILogger<NotificationCleanupService> logger) : BackgroundService
{
	private readonly IDbContextFactory<JordnaerDbContext> _dbContextFactory = dbContextFactory;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			await Task.Delay(TimeSpan.FromHours(24), stoppingToken);

			try
			{
				var retentionDays = configuration.GetValue("Notifications:RetentionDays", 180);

				var deletedCount = await PurgeOldNotificationsAsync(retentionDays, stoppingToken);

				if (deletedCount > 0)
				{
					logger.LogInformation("Purged {DeletedCount} notifications older than {RetentionDays} days", deletedCount, retentionDays);
				}
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				break;
			}
			catch (Exception exception)
			{
				logger.LogError(exception, "Failed to purge old notifications");
			}
		}
	}

	private async Task<int> PurgeOldNotificationsAsync(int retentionDays, CancellationToken ct)
	{
		var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

		await using var context = await _dbContextFactory.CreateDbContextAsync(ct);

		return await context.Notifications
			.Where(n => n.CreatedUtc < cutoff)
			.ExecuteDeleteAsync(ct);
	}
}
