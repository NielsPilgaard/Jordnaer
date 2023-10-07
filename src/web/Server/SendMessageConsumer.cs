using Jordnaer.Server.Database;
using Jordnaer.Shared;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Chat;

public class SendMessageConsumer : IConsumer<SendMessage>
{
    private readonly JordnaerDbContext _context;

    public SendMessageConsumer(JordnaerDbContext context)
    {
        _context = context;
    }

    public async Task Consume(ConsumeContext<SendMessage> consumeContext)
    {
        var chatMessage = consumeContext.Message;

        await using var transaction = await _context.Database.BeginTransactionAsync(consumeContext.CancellationToken);

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

        _context.UnreadMessages.AddRange(recipientIds
            .Where(recipientId => recipientId != chatMessage.SenderId)
            .Select(recipientId => new UnreadMessage
            {
                RecipientId = recipientId,
                ChatId = chatMessage.ChatId,
                MessageSentUtc = chatMessage.SentUtc
            }));

        await _context.Chats
            .Where(chat => chat.Id == chatMessage.ChatId)
            .ExecuteUpdateAsync(call =>
                    call.SetProperty(chat => chat.LastMessageSentUtc, DateTime.UtcNow),
                consumeContext.CancellationToken);

        await _context.SaveChangesAsync(consumeContext.CancellationToken);
        await transaction.CommitAsync(consumeContext.CancellationToken);
    }
}
