using Jordnaer.Shared;
using Refit;

namespace Jordnaer.Client.Features.UserSearch;

public interface IUserSearchClient
{
    [Get("/api/users/search")]
    Task<IApiResponse<UserSearchResult>> GetUsers(UserSearchFilter filter);

    [Get("/api/users/search/autocomplete")]
    Task<IApiResponse<UserSearchResult>> GetUsersWithAutoComplete(string searchString);
}
