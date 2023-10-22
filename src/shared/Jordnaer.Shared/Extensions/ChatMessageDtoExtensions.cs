namespace Jordnaer.Shared;

public static class ChatMessageDtoExtensions
{
    public static ChatMessage ToChatMessage(this ChatMessageDto message) => new()
    {
        Id = message.Id,
        ChatId = message.ChatId,
        SenderId = message.SenderId,
        Text = message.Text,
        AttachmentUrl = message.AttachmentUrl,
        SentUtc = message.SentUtc
    };

    public static SendMessage ToSendMessage(this ChatMessageDto message) => new()
    {
        Id = message.Id,
        ChatId = message.ChatId,
        SenderId = message.SenderId,
        Text = message.Text,
        AttachmentUrl = message.AttachmentUrl,
        SentUtc = message.SentUtc
    };
}
