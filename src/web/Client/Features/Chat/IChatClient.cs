using Jordnaer.Shared;
using Refit;

namespace Jordnaer.Client.Features.Chat;

public interface IChatClient
{
    [Get("/api/chat/{userId}")]
    Task<IApiResponse<List<ChatDto>>> GetChats(string userId, int skip = 0, int take = int.MaxValue);

    [Get("/api/chat/messages/{chatId}")]
    Task<IApiResponse<List<ChatMessageDto>>> GetChatMessages(Guid chatId, int skip = 0, int take = int.MaxValue);

    [Post("/api/chat/start-chat")]
    Task<IApiResponse> StartChat([Body] StartChat startChat);

    [Get("/api/chat/get-chat")]
    Task<IApiResponse<Guid?>> GetChat([Query(CollectionFormat.Multi)] string[] userIds);

    [Post("/api/chat/send-message")]
    Task<IApiResponse> SendMessage([Body] ChatMessageDto message);

    [Post("/api/chat/messages-read/{chatId}")]
    Task<IApiResponse> MarkMessagesAsRead(Guid chatId);
}
