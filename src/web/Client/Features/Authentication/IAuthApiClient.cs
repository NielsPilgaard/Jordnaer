using Jordnaer.Shared.Auth;
using Refit;

namespace Jordnaer.Client.Features.Authentication;

public interface IAuthApiClient
{
    [Get("/api/auth/current-user")]
    Task<IApiResponse<CurrentUserDto?>> GetCurrentUserAsync();

    [Post("/api/auth/login")]
    Task<IApiResponse<bool>> LoginAsync(UserInfo userInfo);

    [Post("/api/auth/register")]
    Task<IApiResponse<bool>> CreateUserAsync(UserInfo userInfo);

    [Post("/api/auth/logout")]
    Task<IApiResponse> LogoutAsync();
}
