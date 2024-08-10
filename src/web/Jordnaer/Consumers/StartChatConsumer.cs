using Jordnaer.Database;
using Jordnaer.Features.Chat;
using Jordnaer.Features.Metrics;
using Jordnaer.Shared;
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

	public StartChatConsumer(IDbContextFactory<JordnaerDbContext> contextFactory,
		ILogger<StartChatConsumer> logger,
		IHubContext<ChatHub, IChatHub> chatHub,
		ChatNotificationService chatNotificationService)
	{
		_contextFactory = contextFactory;
		_logger = logger;
		_chatHub = chatHub;
		_chatNotificationService = chatNotificationService;
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

		JordnaerMetrics.ChatStartedSentCounter.Add(1);
	}
}
