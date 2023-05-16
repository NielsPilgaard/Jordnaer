using Jordnaer.Shared.UserSearch;
using Refit;

namespace Jordnaer.Client.Features.Search;

public interface IUserSearchApi
{
    [Get("/api/users/search")]
    Task<IApiResponse<UserSearchResult>> GetUsers(UserSearchFilter filter);
}
