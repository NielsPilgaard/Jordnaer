using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Features.Chat;
using Jordnaer.Features.Metrics;
using Jordnaer.Shared;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Consumers;

public class SendMessageConsumer(
	JordnaerDbContext context,
	ILogger<SendMessageConsumer> logger,
	IHubContext<ChatHub, IChatHub> chatHub)
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

		JordnaerMetrics.ChatMessagesSentCounter.Add(1);
	}
}
