using Refit;

namespace Jordnaer.Client.Features.Profile;


public interface IUserApiClient
{
    [Delete("/api/users/{id}")]
    Task<IApiResponse> DeleteUserAsync(string id);
}
