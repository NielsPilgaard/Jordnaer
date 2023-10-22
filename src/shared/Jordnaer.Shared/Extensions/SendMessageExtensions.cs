namespace Jordnaer.Shared;

public static class SendMessageExtensions
{
    public static ChatMessageDto ToChatMessageDto(this SendMessage sendMessage) =>
        new()
        {
            ChatId = sendMessage.ChatId,
            Id = sendMessage.Id,
            Text = sendMessage.Text,
            SenderId = sendMessage.SenderId,
            AttachmentUrl = sendMessage.AttachmentUrl,
            SentUtc = sendMessage.SentUtc
        };
}
