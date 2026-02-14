using Jordnaer.Database;
using Jordnaer.Features.Chat;
using Jordnaer.Features.Metrics;
using Jordnaer.Features.Notifications;
using Jordnaer.Shared;
using Jordnaer.Shared.Notifications;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Consumers;

public class StartChatConsumer(
	IDbContextFactory<JordnaerDbContext> contextFactory,
	ILogger<StartChatConsumer> logger,
	IHubContext<ChatHub, IChatHub> chatHub,
	INotificationService notificationService)
	: IConsumer<StartChat>
{
	public async Task Consume(ConsumeContext<StartChat> consumeContext)
	{
		logger.LogInformation("Consuming StartChat message. ChatId: {ChatId}", consumeContext.Message.Id);

		var chat = consumeContext.Message;

		await using var context = await contextFactory.CreateDbContextAsync(consumeContext.CancellationToken);

		context.Chats.Add(new Chat
		{
			LastMessageSentUtc = chat.LastMessageSentUtc,
			Id = chat.Id,
			StartedUtc = chat.StartedUtc,
			Messages = chat.Messages.Select(message => message.ToChatMessage()).ToList(),
			DisplayName = chat.DisplayName
		});

		foreach (var recipient in chat.Recipients)
		{
			context.UserChats.Add(new UserChat
			{
				ChatId = chat.Id,
				UserProfileId = recipient.Id
			});
		}

		try
		{
			await context.SaveChangesAsync(consumeContext.CancellationToken);
			await chatHub.Clients.Users(chat.Recipients.Select(recipient => recipient.Id)).StartChat(chat);
		}
		catch (Exception exception)
		{
			logger.LogError(exception, "Exception occurred while processing {command} command", nameof(StartChat));
			throw;
		}

		// Get email preferences for non-initiator recipients
		var nonInitiatorIds = chat.Recipients
			.Where(r => r.Id != chat.InitiatorId)
			.Select(r => r.Id)
			.ToList();

		var recipientPreferences = await context.UserProfiles
			.AsNoTracking()
			.Where(p => nonInitiatorIds.Contains(p.Id))
			.Select(p => new { p.Id, p.ChatNotificationPreference })
			.ToListAsync(consumeContext.CancellationToken);

		var preferenceLookup = recipientPreferences.ToDictionary(p => p.Id, p => p.ChatNotificationPreference);

		// Create in-app notification for each non-initiator recipient
		var initiator = chat.Recipients.FirstOrDefault(r => r.Id == chat.InitiatorId);
		var senderName = initiator?.DisplayName ?? "Ny bruger";
		var senderImage = initiator?.ProfilePictureUrl;

		foreach (var recipient in chat.Recipients.Where(r => r.Id != chat.InitiatorId))
		{
			try
			{
				// For new chats, send email if preference is FirstMessageOnly or AllMessages
				var preference = preferenceLookup.GetValueOrDefault(recipient.Id, ChatNotificationPreference.FirstMessageOnly);
				var sendEmail = preference is ChatNotificationPreference.FirstMessageOnly
					or ChatNotificationPreference.AllMessages;

				await notificationService.SendAsync(new CreateNotificationRequest
				{
					RecipientId = recipient.Id,
					Title = $"Ny besked fra {senderName}",
					ImageUrl = senderImage,
					LinkUrl = $"/chat/{chat.Id}",
					Type = NotificationType.ChatMessage,
					SourceType = NotificationSourceType.Chat,
					SourceId = chat.Id.ToString(),
					SendEmail = sendEmail,
					EmailSubject = $"Ny besked fra {senderName}"
				}, consumeContext.CancellationToken);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to send in-app notification to recipient {RecipientId} for chat {ChatId}",
					recipient.Id, chat.Id);
			}
		}

		JordnaerMetrics.ChatStartedSentCounter.Add(1);
	}
}
