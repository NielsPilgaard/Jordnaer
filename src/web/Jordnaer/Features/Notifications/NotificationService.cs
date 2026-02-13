using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Features.Email;
using Jordnaer.Shared;
using Jordnaer.Shared.Notifications;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Jordnaer.Features.Notifications;

public interface INotificationService
{
	Task SendAsync(CreateNotificationRequest request, CancellationToken ct = default);
	Task SendToManyAsync(CreateNotificationRequest request, IEnumerable<string> recipientIds, CancellationToken ct = default);
	Task MarkAsReadAsync(string userId, Guid notificationId, CancellationToken ct = default);
	Task MarkAllAsReadAsync(string userId, CancellationToken ct = default);
	Task MarkSourceAsReadAsync(string userId, string sourceType, string sourceId, CancellationToken ct = default);
	Task<List<NotificationDto>> GetUnreadAsync(string userId, int limit = 50, CancellationToken ct = default);
	Task<List<NotificationDto>> GetAllAsync(string userId, int limit = 50, int offset = 0, NotificationType? typeFilter = null, CancellationToken ct = default);
	Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default);
}

public class NotificationService(
	IDbContextFactory<JordnaerDbContext> contextFactory,
	IHubContext<NotificationHub, INotificationHub> hubContext,
	IPublishEndpoint publishEndpoint,
	IOptions<AppOptions> appOptions,
	ILogger<NotificationService> logger) : INotificationService
{
	public async Task SendAsync(CreateNotificationRequest request, CancellationToken ct = default)
	{
		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(ct);

			var notification = new Notification
			{
				Id = Guid.NewGuid(),
				RecipientId = request.RecipientId,
				Title = request.Title,
				Description = request.Description,
				ImageUrl = request.ImageUrl,
				LinkUrl = request.LinkUrl,
				Type = request.Type,
				IsRead = false,
				CreatedUtc = DateTime.UtcNow,
				SourceType = request.SourceType,
				SourceId = request.SourceId
			};

			context.Notifications.Add(notification);
			await context.SaveChangesAsync(ct);

			var dto = ToDto(notification);

			await hubContext.Clients.User(request.RecipientId).ReceiveNotification(dto);

			var unreadCount = await context.Notifications
				.CountAsync(n => n.RecipientId == request.RecipientId && !n.IsRead, ct);
			await hubContext.Clients.User(request.RecipientId).UnreadCountChanged(unreadCount);

			if (request.SendEmail)
			{
				await PublishEmailAsync(request, ct);
			}
		}
		catch (Exception exception)
		{
			logger.LogError(exception,
				"Failed to send notification to {RecipientId}. Type: {Type}, SourceType: {SourceType}, SourceId: {SourceId}",
				request.RecipientId, request.Type, request.SourceType, request.SourceId);
		}
	}

	public async Task SendToManyAsync(CreateNotificationRequest request, IEnumerable<string> recipientIds, CancellationToken ct = default)
	{
		foreach (var recipientId in recipientIds)
		{
			var individualRequest = new CreateNotificationRequest
			{
				RecipientId = recipientId,
				Title = request.Title,
				Description = request.Description,
				ImageUrl = request.ImageUrl,
				LinkUrl = request.LinkUrl,
				Type = request.Type,
				SourceType = request.SourceType,
				SourceId = request.SourceId,
				SendEmail = request.SendEmail,
				EmailSubject = request.EmailSubject
			};

			await SendAsync(individualRequest, ct);
		}
	}

	public async Task MarkAsReadAsync(string userId, Guid notificationId, CancellationToken ct = default)
	{
		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(ct);

			var count = await context.Notifications
				.Where(n => n.Id == notificationId && n.RecipientId == userId && !n.IsRead)
				.ExecuteUpdateAsync(s => s
					.SetProperty(n => n.IsRead, true)
					.SetProperty(n => n.ReadUtc, DateTime.UtcNow), ct);

			if (count > 0)
			{
				await hubContext.Clients.User(userId).NotificationRead(notificationId);

				var unreadCount = await context.Notifications
					.CountAsync(n => n.RecipientId == userId && !n.IsRead, ct);
				await hubContext.Clients.User(userId).UnreadCountChanged(unreadCount);
			}
		}
		catch (Exception exception)
		{
			logger.LogError(exception, "Failed to mark notification {NotificationId} as read for user {UserId}",
				notificationId, userId);
		}
	}

	public async Task MarkAllAsReadAsync(string userId, CancellationToken ct = default)
	{
		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(ct);

			await context.Notifications
				.Where(n => n.RecipientId == userId && !n.IsRead)
				.ExecuteUpdateAsync(s => s
					.SetProperty(n => n.IsRead, true)
					.SetProperty(n => n.ReadUtc, DateTime.UtcNow), ct);

			await hubContext.Clients.User(userId).UnreadCountChanged(0);
		}
		catch (Exception exception)
		{
			logger.LogError(exception, "Failed to mark all notifications as read for user {UserId}", userId);
		}
	}

	public async Task MarkSourceAsReadAsync(string userId, string sourceType, string sourceId, CancellationToken ct = default)
	{
		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(ct);

			await context.Notifications
				.Where(n => n.RecipientId == userId &&
							n.SourceType == sourceType &&
							n.SourceId == sourceId &&
							!n.IsRead)
				.ExecuteUpdateAsync(s => s
					.SetProperty(n => n.IsRead, true)
					.SetProperty(n => n.ReadUtc, DateTime.UtcNow), ct);

			await hubContext.Clients.User(userId).NotificationsCleared(sourceType, sourceId);

			var unreadCount = await context.Notifications
				.CountAsync(n => n.RecipientId == userId && !n.IsRead, ct);
			await hubContext.Clients.User(userId).UnreadCountChanged(unreadCount);
		}
		catch (Exception exception)
		{
			logger.LogError(exception,
				"Failed to mark source notifications as read for user {UserId}. SourceType: {SourceType}, SourceId: {SourceId}",
				userId, sourceType, sourceId);
		}
	}

	public async Task<List<NotificationDto>> GetUnreadAsync(string userId, int limit = 50, CancellationToken ct = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(ct);

		return await context.Notifications
			.AsNoTracking()
			.Where(n => n.RecipientId == userId && !n.IsRead)
			.OrderByDescending(n => n.CreatedUtc)
			.Take(limit)
			.Select(n => new NotificationDto
			{
				Id = n.Id,
				RecipientId = n.RecipientId,
				Title = n.Title,
				Description = n.Description,
				ImageUrl = n.ImageUrl,
				LinkUrl = n.LinkUrl,
				Type = n.Type,
				IsRead = n.IsRead,
				CreatedUtc = n.CreatedUtc,
				ReadUtc = n.ReadUtc,
				SourceType = n.SourceType,
				SourceId = n.SourceId
			})
			.ToListAsync(ct);
	}

	public async Task<List<NotificationDto>> GetAllAsync(string userId, int limit = 50, int offset = 0, NotificationType? typeFilter = null, CancellationToken ct = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(ct);

		var query = context.Notifications
			.AsNoTracking()
			.Where(n => n.RecipientId == userId);

		if (typeFilter.HasValue)
		{
			query = query.Where(n => n.Type == typeFilter.Value);
		}

		return await query
			.OrderByDescending(n => n.CreatedUtc)
			.Skip(offset)
			.Take(limit)
			.Select(n => new NotificationDto
			{
				Id = n.Id,
				RecipientId = n.RecipientId,
				Title = n.Title,
				Description = n.Description,
				ImageUrl = n.ImageUrl,
				LinkUrl = n.LinkUrl,
				Type = n.Type,
				IsRead = n.IsRead,
				CreatedUtc = n.CreatedUtc,
				ReadUtc = n.ReadUtc,
				SourceType = n.SourceType,
				SourceId = n.SourceId
			})
			.ToListAsync(ct);
	}

	public async Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(ct);

		return await context.Notifications
			.AsNoTracking()
			.CountAsync(n => n.RecipientId == userId && !n.IsRead, ct);
	}

	private async Task PublishEmailAsync(CreateNotificationRequest request, CancellationToken ct)
	{
		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(ct);

			var user = await context.Users
				.AsNoTracking()
				.Where(u => u.Id == request.RecipientId && !string.IsNullOrEmpty(u.Email))
				.Select(u => new { u.Email, u.UserName })
				.FirstOrDefaultAsync(ct);

			if (user is null)
			{
				return;
			}

			var email = new SendEmail
			{
				Subject = request.EmailSubject ?? request.Title,
				HtmlContent = EmailContentBuilder.GenericNotification(request.Title, request.Description, request.LinkUrl, appOptions.Value.BaseUrl),
				To = [new EmailRecipient { Email = user.Email!, DisplayName = user.UserName }]
			};

			await publishEndpoint.Publish(email, ct);
		}
		catch (Exception exception)
		{
			logger.LogError(exception, "Failed to publish notification email for user {RecipientId}", request.RecipientId);
		}
	}

	private static NotificationDto ToDto(Notification notification) => new()
	{
		Id = notification.Id,
		RecipientId = notification.RecipientId,
		Title = notification.Title,
		Description = notification.Description,
		ImageUrl = notification.ImageUrl,
		LinkUrl = notification.LinkUrl,
		Type = notification.Type,
		IsRead = notification.IsRead,
		CreatedUtc = notification.CreatedUtc,
		ReadUtc = notification.ReadUtc,
		SourceType = notification.SourceType,
		SourceId = notification.SourceId
	};
}
