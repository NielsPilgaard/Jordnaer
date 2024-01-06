using Jordnaer.Shared;
using Refit;

namespace Jordnaer.Features.Groups;

public interface IGroupChatClient
{
	[Post("/api/group-chat/start-chat")]
	//TODO: Update to StartGroupChat command
	Task<IApiResponse> StartGroupChat([Body] StartChat startChat);
}
