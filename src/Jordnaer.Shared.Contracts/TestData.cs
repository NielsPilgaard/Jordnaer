namespace Jordnaer.Shared.Contracts;

public static class TestData
{
    public static List<ChatUserDto> ChatUserDtos => new()
    {
        new ChatUserDto { Id = "1", DisplayName = "User 1", ProfilePictureUrl = "https://example.com/user1.jpg" },
        new ChatUserDto { Id = "2", DisplayName = "User 2", ProfilePictureUrl = "https://example.com/user2.jpg" },
        new ChatUserDto { Id = "3", DisplayName = "User 3", ProfilePictureUrl = "https://example.com/user3.jpg" }
    };

    public static List<ChatMessageDto> ChatMessageDtos => new()
    {
        new ChatMessageDto { Sender = ChatUserDtos[0], Text = "Hello from User 1", IsDeleted = false, SentUtc = DateTime.UtcNow, AttachmentUrls = new string[] { "https://example.com/doc1.pdf" } },
        new ChatMessageDto { Sender = ChatUserDtos[1], Text = "Hello from User 2", IsDeleted = false, SentUtc = DateTime.UtcNow, AttachmentUrls = new string[] { "https://example.com/doc2.pdf" } },
        new ChatMessageDto { Sender = ChatUserDtos[2], Text = "Hello from User 3", IsDeleted = false, SentUtc = DateTime.UtcNow, AttachmentUrls = new string[] { "https://example.com/doc3.pdf" } }
    };

    public static List<ChatDto> ChatDtos => new()
    {
        new ChatDto { Id = Guid.NewGuid(), DisplayName = "Chat 1", Messages = ChatMessageDtos, Recipients = ChatUserDtos, LastMessageSentUtc = DateTime.UtcNow, StartedUtc = DateTime.UtcNow },
        new ChatDto { Id = Guid.NewGuid(), DisplayName = "Chat 2", Messages = ChatMessageDtos, Recipients = ChatUserDtos, LastMessageSentUtc = DateTime.UtcNow, StartedUtc = DateTime.UtcNow },
        new ChatDto { Id = Guid.NewGuid(), DisplayName = "Chat 3", Messages = ChatMessageDtos, Recipients = ChatUserDtos, LastMessageSentUtc = DateTime.UtcNow, StartedUtc = DateTime.UtcNow }
    };
}
