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
	ChatNotificationService chatNotificationService,
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

		foreach (var recipientId in recipientIds.Where(recipientId => recipientId != chatMessage.SenderId))
		{
			context.UnreadMessages.Add(new UnreadMessage
			{
				RecipientId = recipientId,
				ChatId = chatMessage.ChatId,
				MessageSentUtc = chatMessage.SentUtc
			});
		}

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

		// Send email notifications to users who want all messages
		await chatNotificationService.NotifyRecipientsOfNewMessage(chatMessage, consumeContext.CancellationToken);

		// Create in-app notification for each non-sender recipient
		var sender = await context.UserProfiles
			.AsNoTracking()
			.Where(p => p.Id == chatMessage.SenderId)
			.Select(p => new { p.DisplayName, p.ProfilePictureUrl })
			.FirstOrDefaultAsync(consumeContext.CancellationToken);

		var senderName = sender?.DisplayName ?? "Ny bruger";
		var messagePreview = chatMessage.Text.Length > 50
			? chatMessage.Text[..50] + "..."
			: chatMessage.Text;

		foreach (var recipientId in recipientIds.Where(id => id != chatMessage.SenderId))
		{
			await notificationService.SendAsync(new CreateNotificationRequest
			{
				RecipientId = recipientId,
				Title = $"Ny besked fra {senderName}",
				Description = messagePreview,
				ImageUrl = sender?.ProfilePictureUrl,
				LinkUrl = $"/chat/{chatMessage.ChatId}",
				Type = NotificationType.ChatMessage,
				SourceType = NotificationSourceType.Chat,
				SourceId = chatMessage.ChatId.ToString()
			}, consumeContext.CancellationToken);
		}

		JordnaerMetrics.ChatMessagesSentCounter.Add(1);
	}
}
