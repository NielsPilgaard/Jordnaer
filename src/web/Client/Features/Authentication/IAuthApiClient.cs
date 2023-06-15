using Jordnaer.Shared.Auth;
using Refit;

namespace Jordnaer.Client.Features.Authentication;

public interface IAuthApiClient
{
    [Get("/api/auth/current-user")]
    Task<IApiResponse<CurrentUserDto?>> GetCurrentUserAsync();

    [Post("/api/auth/login")]
    Task<bool> LoginAsync(UserInfo userInfo);

    [Post("/api/auth/register")]
    Task<bool> CreateUserAsync(UserInfo userInfo);

    [Post("/api/auth/logout")]
    Task<bool> LogoutAsync();
}
