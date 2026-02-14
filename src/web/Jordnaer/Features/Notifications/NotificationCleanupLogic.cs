using Jordnaer.Database;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Features.Notifications;

public interface INotificationCleanupLogic
{
	Task<int> PurgeOldNotificationsAsync(int retentionDays, CancellationToken ct = default);
}

public class NotificationCleanupLogic(IDbContextFactory<JordnaerDbContext> contextFactory) : INotificationCleanupLogic
{
	public async Task<int> PurgeOldNotificationsAsync(int retentionDays, CancellationToken ct = default)
	{
		// Never delete notifications younger than 7 days to avoid issues with users missing important notifications
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(retentionDays, 7);

		var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
		await using var context = await contextFactory.CreateDbContextAsync(ct);

		return await context.Notifications
			.Where(n => n.CreatedUtc < cutoff)
			.ExecuteDeleteAsync(ct);
	}
}
