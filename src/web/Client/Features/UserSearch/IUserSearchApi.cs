using Jordnaer.Shared.UserSearch;
using Refit;

namespace Jordnaer.Client.Features.UserSearch;

public interface IUserSearchApi
{
    [Get("/api/users/search")]
    Task<IApiResponse<UserSearchResult>> GetUsers(UserSearchFilter filter);
}
