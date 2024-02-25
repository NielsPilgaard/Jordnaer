using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Features.Chat;
using Jordnaer.Shared;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Consumers;

public class SendMessageConsumer : IConsumer<SendMessage>
{
	private readonly JordnaerDbContext _context;
	private readonly ILogger<SendMessageConsumer> _logger;
	private readonly IHubContext<ChatHub, IChatHub> _chatHub;

	public SendMessageConsumer(JordnaerDbContext context, ILogger<SendMessageConsumer> logger, IHubContext<ChatHub, IChatHub> chatHub)
	{
		_context = context;
		_logger = logger;
		_chatHub = chatHub;
	}

	public async Task Consume(ConsumeContext<SendMessage> consumeContext)
	{
		var chatMessage = consumeContext.Message;

		_context.ChatMessages.Add(
			new ChatMessage
			{
				ChatId = chatMessage.ChatId,
				Id = chatMessage.Id,
				SenderId = chatMessage.SenderId,
				Text = chatMessage.Text,
				AttachmentUrl = chatMessage.AttachmentUrl,
				SentUtc = chatMessage.SentUtc
			});

		var recipientIds = await _context.UserChats
			.Where(userChat => userChat.ChatId == chatMessage.ChatId)
			.Select(userChat => userChat.UserProfileId)
			.ToListAsync(consumeContext.CancellationToken);

		foreach (var recipientId in recipientIds.Where(recipientId => recipientId != chatMessage.SenderId))
		{
			_context.UnreadMessages.Add(new UnreadMessage
			{
				RecipientId = recipientId,
				ChatId = chatMessage.ChatId,
				MessageSentUtc = chatMessage.SentUtc
			});
		}

		try
		{
			await _context.SaveChangesAsync(consumeContext.CancellationToken);

			await _chatHub.Clients.Users(recipientIds).ReceiveChatMessage(chatMessage);

			await _context.Chats
				.Where(chat => chat.Id == chatMessage.ChatId)
				.ExecuteUpdateAsync(call =>
						call.SetProperty(chat => chat.LastMessageSentUtc, DateTime.UtcNow),
					consumeContext.CancellationToken);
		}
		catch (Exception exception)
		{
			_logger.LogException(exception);
			throw;
		}
	}
}
