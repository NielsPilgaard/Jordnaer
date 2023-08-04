namespace Jordnaer.Shared;

public class ChatMessageDto
{
    public required Guid Id { get; set; }

    public required ChatUserDto Sender { get; init; }

    public required string Text { get; init; }

    public bool IsDeleted { get; init; } = false;

    public DateTime SentUtc { get; init; } = DateTime.UtcNow;

    public string? AttachmentUrl { get; init; }
}
