using System.Security.Claims;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Components.Authorization;

namespace Jordnaer.Client.Features.Authentication;

public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly IAuthApiClient _authApiClient;
    private readonly ILogger<AuthStateProvider> _logger;
    private AuthenticationState _currentAuthenticationState;
    private bool _authenticationStateChanged = true;

    public AuthStateProvider(IAuthApiClient authApiClient, ILogger<AuthStateProvider> logger)
    {
        _authApiClient = authApiClient;
        _logger = logger;
        _currentAuthenticationState = new AuthenticationState(new ClaimsPrincipal());
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // If auth state is unchanged, return cached auth state
        if (_authenticationStateChanged is false)
            return _currentAuthenticationState;

        CurrentUserDto? currentUser;
        var response = await _authApiClient.GetCurrentUserAsync();
        if (response.IsSuccessStatusCode)
        {
            currentUser = response.Content;
        }
        else
        {
            _logger.LogError(response.Error, "Exception occurred while getting user authentication state");
            return _currentAuthenticationState;
        }

        var currentClaimsPrincipal = currentUser?.ToClaimsPrincipal() ?? new ClaimsPrincipal();

        _currentAuthenticationState = new AuthenticationState(currentClaimsPrincipal);

        // Reset auth state changed to false, now that we've updated it
        _authenticationStateChanged = false;

        return _currentAuthenticationState;
    }

    public async Task<bool> LoginAsync(UserInfo userInfo)
    {
        var response = await _authApiClient.LoginAsync(userInfo);
        if (response is { IsSuccessStatusCode: true, Content: true })
        {
            _authenticationStateChanged = true;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        return false;
    }

    public async Task<bool> CreateUserAsync(UserInfo userInfo)
    {
        var response = await _authApiClient.CreateUserAsync(userInfo);
        if (response is { IsSuccessStatusCode: true, Content: true })
        {
            _authenticationStateChanged = true;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        return false;
    }

    public async Task<bool> LogoutAsync()
    {
        var response = await _authApiClient.LogoutAsync();
        if (response.IsSuccessStatusCode)
        {
            _authenticationStateChanged = true;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        return false;
    }
}
