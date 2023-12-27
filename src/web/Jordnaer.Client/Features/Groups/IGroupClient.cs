using Jordnaer.Shared;
using Refit;

namespace Jordnaer.Client.Features.Groups;

public interface IGroupClient
{
    [Get("/api/groups/{id}")]
    Task<IApiResponse<Group?>> GetGroupByIdAsync(Guid id);

    [Get("/api/groups/slim/{name}")]

    Task<IApiResponse<GroupSlim?>> GetSlimGroupByNameAsync(string name);

    [Get("/api/groups/slim")]
    Task<IApiResponse<List<UserGroupAccess>>> GetSlimGroupsForUserAsync();

    [Post("/api/groups")]
    Task<IApiResponse> CreateGroupAsync([Body] Group group);

    [Put("/api/groups/{id}")]
    Task<IApiResponse> UpdateGroupAsync(Guid id, [Body] Group group);

    [Delete("/api/groups/{id}")]
    Task<IApiResponse> DeleteGroupAsync(Guid id);
}
