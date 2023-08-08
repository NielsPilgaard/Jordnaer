using Jordnaer.Shared;
using Refit;

namespace Jordnaer.Client.Features.Chat;

public interface IChatClient
{
    [Get($"/api/chat")]
    Task<IApiResponse<List<ChatDto>>> GetChats(string userId);

    [Get($"/api/chat/messages")]
    Task<IApiResponse<List<ChatMessage>>> GetChat(Guid chatId, int page, int pageSize);

    [Post($"/api/chat/{MessagingConstants.StartChat}")]
    Task<IApiResponse> StartChat([Body] ChatDto chat);

    [Post($"/api/chat/{MessagingConstants.SendMessage}")]
    Task<IApiResponse> SendMessage([Body] ChatMessageDto message);
}
