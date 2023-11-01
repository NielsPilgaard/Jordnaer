using Jordnaer.Shared;
using Refit;

namespace Jordnaer.Client.Features.Groups;

public interface IGroupClient
{
    [Get("/api/groups/{id}")]
    Task<IApiResponse<GroupDto>> GetGroupByIdAsync(Guid id);

    [Post("/api/groups")]
    Task<IApiResponse> CreateGroupAsync([Body] Group group);

    [Put("/api/groups/{id}")]
    Task<IApiResponse> UpdateGroupAsync(Guid id, [Body] Group group);

    [Delete("/api/groups/{id}")]
    Task<IApiResponse> DeleteGroupAsync(Guid id);
}
