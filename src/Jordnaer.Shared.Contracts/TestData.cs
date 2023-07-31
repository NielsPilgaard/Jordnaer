namespace Jordnaer.Shared.Contracts;

public static class TestData
{
    public static List<ChatUserDto> ChatUserDtos(UserProfile ownProfile) => new()
    {
        new ChatUserDto { Id = ownProfile.Id, DisplayName =$"{ownProfile.FirstName} {ownProfile.LastName}", ProfilePictureUrl = ownProfile.ProfilePictureUrl },
        new ChatUserDto { Id = "2", DisplayName = "User 2", ProfilePictureUrl = "https://example.com/user2.jpg" },
        new ChatUserDto { Id = "3", DisplayName = "User 3", ProfilePictureUrl = "https://example.com/user3.jpg" }
    };

    public static List<ChatMessageDto> ChatMessageDtos(UserProfile ownProfile) => new()
    {
        new ChatMessageDto { Sender = ChatUserDtos(ownProfile)[0], Text = "Hello from me", IsDeleted = false, SentUtc = DateTime.UtcNow, AttachmentUrl = "https://example.com/doc1.pdf"},
        new ChatMessageDto { Sender = ChatUserDtos(ownProfile)[1], Text = "Hello from User 2", IsDeleted = false, SentUtc = DateTime.UtcNow, AttachmentUrl =  "https://example.com/doc2.pdf"  },
        new ChatMessageDto { Sender = ChatUserDtos(ownProfile)[2], Text = "Hello from User 3", IsDeleted = false, SentUtc = DateTime.UtcNow, AttachmentUrl =  "https://example.com/doc3.pdf"  }
    };

    public static List<ChatDto> ChatDtos(UserProfile ownProfile) => new()
    {
        new ChatDto { Id = Guid.NewGuid(), DisplayName = "Chat 1", Messages = ChatMessageDtos(ownProfile), Recipients = ChatUserDtos(ownProfile), LastMessageSentUtc = DateTime.UtcNow, StartedUtc = DateTime.UtcNow },
        new ChatDto { Id = Guid.NewGuid(), DisplayName = "Chat 2", Messages = ChatMessageDtos(ownProfile), Recipients = ChatUserDtos(ownProfile), LastMessageSentUtc = DateTime.UtcNow, StartedUtc = DateTime.UtcNow },
        new ChatDto { Id = Guid.NewGuid(), DisplayName = "Chat 3", Messages = ChatMessageDtos(ownProfile), Recipients = ChatUserDtos(ownProfile), LastMessageSentUtc = DateTime.UtcNow, StartedUtc = DateTime.UtcNow }
    };
}
