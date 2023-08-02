using Jordnaer.Shared.Contracts;
using Refit;

namespace Jordnaer.Client.Features.Chat;

public interface IChatApiClient
{
    [Post($"/api/chat/{MessagingConstants.StartChat}")]
    Task<IApiResponse> StartChat([Body] ChatDto chat);

    [Post($"/api/chat/{MessagingConstants.SendMessage}")]
    Task<IApiResponse> SendMessage([Body] ChatMessageDto message);
}
