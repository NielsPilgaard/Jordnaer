using Jordnaer.Shared;
using MassTransit;

namespace Jordnaer.Chat;

public class StartChatConsumer : IConsumer<StartChat>
{
    private readonly JordnaerDbContext _context;

    public StartChatConsumer(JordnaerDbContext context)
    {
        _context = context;
    }

    public async Task Consume(ConsumeContext<StartChat> consumeContext)
    {
        var chat = consumeContext.Message;

        _context.Chats.Add(new Shared.Chat
        {
            LastMessageSentUtc = chat.LastMessageSentUtc,
            Id = chat.Id,
            StartedUtc = chat.StartedUtc,
            Messages = chat.Messages.Select(static message => message.ToChatMessage()).ToList()
        });

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

        foreach (var recipient in chat.Recipients)
        {
            _context.UserChats.Add(new UserChat
            {
                ChatId = chat.Id,
                UserProfileId = recipient.Id
            });
        }

        await _context.SaveChangesAsync(consumeContext.CancellationToken);
    }
}
