using Jordnaer.Shared;
using Refit;

namespace Jordnaer.Client.Features.Chat;

public interface IChatClient
{
    [Get("/api/chat")]
    Task<IApiResponse<ChatResult>> GetChats(string userId, int page = 1, int pageSize = 15);

    [Get($"/api/chat/{MessagingConstants.GetChatMessages}")]
    Task<IApiResponse<ChatMessageResult>> GetChat(Guid chatId, int page = 1, int pageSize = 15);

    [Post($"/api/chat/{MessagingConstants.StartChat}")]
    Task<IApiResponse> StartChat([Body] ChatDto chat);

    [Post($"/api/chat/{MessagingConstants.SendMessage}")]
    Task<IApiResponse> SendMessage([Body] ChatMessageDto message);
}
