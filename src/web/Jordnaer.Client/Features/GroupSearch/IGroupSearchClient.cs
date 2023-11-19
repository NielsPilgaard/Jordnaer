using Jordnaer.Shared;
using Refit;

namespace Jordnaer.Client.Features.GroupSearch;

public interface IGroupSearchClient
{

    [Get("/api/groups/search")]
    Task<IApiResponse<GroupSearchResult>> GetGroups(GroupSearchFilter filter);
}
