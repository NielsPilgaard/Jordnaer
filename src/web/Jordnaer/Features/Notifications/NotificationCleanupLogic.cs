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
		var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

		await using var context = await contextFactory.CreateDbContextAsync(ct);

		return await context.Notifications
			.Where(n => n.CreatedUtc < cutoff)
			.ExecuteDeleteAsync(ct);
	}
}
