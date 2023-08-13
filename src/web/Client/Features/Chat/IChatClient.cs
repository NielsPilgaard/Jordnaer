using Jordnaer.Shared;
using Refit;

namespace Jordnaer.Client.Features.Chat;

public interface IChatClient
{
    [Get("/api/chat/{userId}")]
    Task<IApiResponse<List<ChatDto>>> GetChats(string userId, int skip = 0, int take = int.MaxValue);

    [Get($"/api/chat/{MessagingConstants.GetChatMessages}/{{chatId}}")]
    Task<IApiResponse<List<ChatMessageDto>>> GetChatMessages(Guid chatId, int skip = 0, int take = 15);

    [Post($"/api/chat/{MessagingConstants.StartChat}")]
    Task<IApiResponse> StartChat([Body] ChatDto chat);

    [Post($"/api/chat/{MessagingConstants.SendMessage}")]
    Task<IApiResponse> SendMessage([Body] ChatMessageDto message);
}
