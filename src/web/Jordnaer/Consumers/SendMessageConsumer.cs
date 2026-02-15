using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Features.Chat;
using Jordnaer.Features.Metrics;
using Jordnaer.Features.Notifications;
using Jordnaer.Shared;
using Jordnaer.Shared.Notifications;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Consumers;

public class SendMessageConsumer(
	JordnaerDbContext context,
	ILogger<SendMessageConsumer> logger,
	IHubContext<ChatHub, IChatHub> chatHub,
	INotificationService notificationService)
	: IConsumer<SendMessage>
{
	public async Task Consume(ConsumeContext<SendMessage> consumeContext)
	{
		logger.LogDebug("Consuming SendMessage message. ChatId: {ChatId}", consumeContext.Message.ChatId);

		var chatMessage = consumeContext.Message;

		context.ChatMessages.Add(
			new ChatMessage
			{
				ChatId = chatMessage.ChatId,
				Id = chatMessage.Id,
				SenderId = chatMessage.SenderId,
				Text = chatMessage.Text,
				AttachmentUrl = chatMessage.AttachmentUrl,
				SentUtc = chatMessage.SentUtc
			});

		var recipientIds = await context.UserChats
			.Where(userChat => userChat.ChatId == chatMessage.ChatId)
			.Select(userChat => userChat.UserProfileId)
			.ToListAsync(consumeContext.CancellationToken);

		try
		{
			await context.SaveChangesAsync(consumeContext.CancellationToken);

			await chatHub.Clients.Users(recipientIds).ReceiveChatMessage(chatMessage);

			await context.Chats
				.Where(chat => chat.Id == chatMessage.ChatId)
				.ExecuteUpdateAsync(call =>
						call.SetProperty(chat => chat.LastMessageSentUtc, DateTime.UtcNow),
					consumeContext.CancellationToken);
		}
		catch (Exception exception)
		{
			logger.LogException(exception);
			throw;
		}

		// Get sender info for notification
		var sender = await context.UserProfiles
			.AsNoTracking()
			.Where(p => p.Id == chatMessage.SenderId)
			.Select(p => new { p.DisplayName, p.ProfilePictureUrl })
			.FirstOrDefaultAsync(consumeContext.CancellationToken);

		var senderName = sender?.DisplayName ?? "Ny bruger";
		var messageText = chatMessage.Text ?? string.Empty;
		var messagePreview = messageText.Length > 50
			? messageText[..50] + "..."
			: messageText;

		try
		{
			// Get chat notification preferences for all non-sender recipients
			var recipientPreferences = await context.UserProfiles
				.AsNoTracking()
				.Where(p => recipientIds.Contains(p.Id) && p.Id != chatMessage.SenderId)
				.Select(p => new { p.Id, p.ChatNotificationPreference })
				.ToListAsync(consumeContext.CancellationToken);

			var recipientIdsToCheck = recipientPreferences.Select(r => r.Id).ToList();
			var alreadyNotifiedRecipientIds = (await context.Notifications
				.AsNoTracking()
				.Where(n => recipientIdsToCheck.Contains(n.RecipientId)
					&& n.SourceType == NotificationSourceType.Chat
					&& n.SourceId == chatMessage.ChatId.ToString()
					&& !n.IsRead)
				.Select(n => n.RecipientId)
				.ToListAsync(consumeContext.CancellationToken))
				.ToHashSet();

			foreach (var recipient in recipientPreferences)
			{
				if (alreadyNotifiedRecipientIds.Contains(recipient.Id))
				{
					continue;
				}

				// Only send email for follow-up messages if preference is AllMessages
				var sendEmail = recipient.ChatNotificationPreference == ChatNotificationPreference.AllMessages;

				await notificationService.SendAsync(new CreateNotificationRequest
				{
					RecipientId = recipient.Id,
					Title = $"Ny besked fra {senderName}",
					Description = messagePreview,
					ImageUrl = sender?.ProfilePictureUrl,
					LinkUrl = $"/chat/{chatMessage.ChatId}",
					Type = NotificationType.ChatMessage,
					SourceType = NotificationSourceType.Chat,
					SourceId = chatMessage.ChatId.ToString(),
					SendEmail = sendEmail,
					EmailSubject = $"Ny besked fra {senderName}"
				}, consumeContext.CancellationToken);
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to send in-app notifications for ChatId {ChatId}", chatMessage.ChatId);
		}

		JordnaerMetrics.ChatMessagesSentCounter.Add(1);
	}
}
