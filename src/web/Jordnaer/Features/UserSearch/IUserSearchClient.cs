using Jordnaer.Shared;
using Refit;

namespace Jordnaer.Features.UserSearch;

public interface IUserSearchClient
{
	[Get("/api/users/search")]
	Task<IApiResponse<UserSearchResult>> GetUsers(UserSearchFilter filter);

	[Get("/api/users/search/autocomplete")]
	Task<IApiResponse<List<UserSlim>>> GetUsersWithAutoComplete(string searchString, CancellationToken cancellationToken = default);
}
