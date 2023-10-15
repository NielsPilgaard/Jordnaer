using Jordnaer.Shared;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Chat;

public class SendMessageConsumer : IConsumer<SendMessage>
{
    private readonly ChatDbContext _context;

    public SendMessageConsumer(ChatDbContext context)
    {
        _context = context;
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

        foreach (string recipientId in recipientIds.Where(recipientId => recipientId != chatMessage.SenderId))
        {
            _context.UnreadMessages.Add(new UnreadMessage
            {
                RecipientId = recipientId,
                ChatId = chatMessage.ChatId,
                MessageSentUtc = chatMessage.SentUtc
            });
        }

        await _context.SaveChangesAsync(consumeContext.CancellationToken);

        await _context.Chats
            .Where(chat => chat.Id == chatMessage.ChatId)
            .ExecuteUpdateAsync(call =>
                    call.SetProperty(chat => chat.LastMessageSentUtc, DateTime.UtcNow),
                consumeContext.CancellationToken);
    }
}
