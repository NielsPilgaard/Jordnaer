using Jordnaer.Database;
using Jordnaer.Features.Chat;
using Jordnaer.Shared;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace Jordnaer.Consumers;

public class StartChatConsumer : IConsumer<StartChat>
{
	private readonly JordnaerDbContext _context;
	private readonly ILogger<StartChatConsumer> _logger;
	private readonly IHubContext<ChatHub, IChatHub> _chatHub;

	public StartChatConsumer(JordnaerDbContext context, ILogger<StartChatConsumer> logger, IHubContext<ChatHub, IChatHub> chatHub)
	{
		_context = context;
		_logger = logger;
		_chatHub = chatHub;
	}

	public async Task Consume(ConsumeContext<StartChat> consumeContext)
	{
		var chat = consumeContext.Message;

		_context.Chats.Add(new Chat
		{
			LastMessageSentUtc = chat.LastMessageSentUtc,
			Id = chat.Id,
			StartedUtc = chat.StartedUtc,
			Messages = chat.Messages.Select(message => message.ToChatMessage()).ToList(),
			DisplayName = chat.DisplayName
		});

		foreach (var recipient in chat.Recipients)
		{
			_context.UserChats.Add(new UserChat
			{
				ChatId = chat.Id,
				UserProfileId = recipient.Id
			});
		}

		foreach (var message in chat.Messages)
		{
			foreach (var recipient in chat.Recipients.Where(recipient => recipient.Id != chat.InitiatorId))
			{
				_context.UnreadMessages.Add(new UnreadMessage
				{
					RecipientId = recipient.Id,
					ChatId = chat.Id,
					MessageSentUtc = message.SentUtc
				});
			}
		}

		try
		{
			await _context.SaveChangesAsync(consumeContext.CancellationToken);
			await _chatHub.Clients.Users(chat.Recipients.Select(recipient => recipient.Id)).StartChat(chat);
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "Exception occurred while processing {command} command", nameof(StartChat));
			throw;
		}
	}
}
