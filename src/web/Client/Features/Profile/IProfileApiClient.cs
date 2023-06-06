using Jordnaer.Shared;
using Refit;

namespace Jordnaer.Client.Features.Profile;

public interface IProfileApiClient
{
    [Get("/api/profiles/{username}")]
    Task<IApiResponse<ProfileDto>> GetUserProfile(string userName);
    [Get("/api/profiles")]
    Task<IApiResponse<UserProfile>> GetUserProfile();
    [Put("/api/profiles")]
    Task<IApiResponse<UserProfile>> UpdateUserProfile(UserProfile userProfile);
}
