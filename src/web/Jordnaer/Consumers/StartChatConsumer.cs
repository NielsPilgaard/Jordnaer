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

public class StartChatConsumer : IConsumer<StartChat>
{
	private readonly IDbContextFactory<JordnaerDbContext> _contextFactory;
	private readonly ILogger<StartChatConsumer> _logger;
	private readonly IHubContext<ChatHub, IChatHub> _chatHub;
	private readonly ChatNotificationService _chatNotificationService;
	private readonly INotificationService _notificationService;

	public StartChatConsumer(IDbContextFactory<JordnaerDbContext> contextFactory,
		ILogger<StartChatConsumer> logger,
		IHubContext<ChatHub, IChatHub> chatHub,
		ChatNotificationService chatNotificationService,
		INotificationService notificationService)
	{
		_contextFactory = contextFactory;
		_logger = logger;
		_chatHub = chatHub;
		_chatNotificationService = chatNotificationService;
		_notificationService = notificationService;
	}

	public async Task Consume(ConsumeContext<StartChat> consumeContext)
	{
		_logger.LogInformation("Consuming StartChat message. ChatId: {ChatId}", consumeContext.Message.Id);

		var chat = consumeContext.Message;

		await using var context = await _contextFactory.CreateDbContextAsync(consumeContext.CancellationToken);

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

		foreach (var message in chat.Messages)
		{
			foreach (var recipient in chat.Recipients.Where(recipient => recipient.Id != chat.InitiatorId))
			{
				context.UnreadMessages.Add(new UnreadMessage
				{
					RecipientId = recipient.Id,
					ChatId = chat.Id,
					MessageSentUtc = message.SentUtc
				});
			}
		}

		try
		{
			await context.SaveChangesAsync(consumeContext.CancellationToken);
			await _chatHub.Clients.Users(chat.Recipients.Select(recipient => recipient.Id)).StartChat(chat);
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "Exception occurred while processing {command} command", nameof(StartChat));
			throw;
		}

		await _chatNotificationService.NotifyRecipients(chat);

		// Create in-app notification for each non-initiator recipient
		var initiator = chat.Recipients.FirstOrDefault(r => r.Id == chat.InitiatorId);
		var senderName = initiator?.DisplayName ?? "Ny bruger";
		var senderImage = initiator?.ProfilePictureUrl;

		foreach (var recipient in chat.Recipients.Where(r => r.Id != chat.InitiatorId))
		{
			try
			{
				await _notificationService.SendAsync(new CreateNotificationRequest
				{
					RecipientId = recipient.Id,
					Title = $"Ny besked fra {senderName}",
					ImageUrl = senderImage,
					LinkUrl = $"/chat/{chat.Id}",
					Type = NotificationType.ChatMessage,
					SourceType = NotificationSourceType.Chat,
					SourceId = chat.Id.ToString()
				}, consumeContext.CancellationToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send in-app notification to recipient {RecipientId} for chat {ChatId}",
					recipient.Id, chat.Id);
			}
		}

		JordnaerMetrics.ChatStartedSentCounter.Add(1);
	}
}
