using Jordnaer.Shared;
using Refit;

namespace Jordnaer.Client.Features.UserSearch;

public interface IUserSearchApiClient
{
    [Get("/api/users/search")]
    Task<IApiResponse<UserSearchResult>> GetUsers(UserSearchFilter filter);
}