using System.Security.Claims;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Components.Authorization;

namespace Jordnaer.Client.Authentication;

public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly AuthClient _authClient;
    private readonly UserClient _userClient;
    private readonly ILogger<AuthStateProvider> _logger;
    private AuthenticationState _currentAuthenticationState;
    private bool _authenticationStateChanged = true;

    public AuthStateProvider(AuthClient authClient, ILogger<AuthStateProvider> logger, UserClient userClient)
    {
        _authClient = authClient;
        _logger = logger;
        _userClient = userClient;
        _currentAuthenticationState = new AuthenticationState(new ClaimsPrincipal());
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // If auth state is unchanged, return cached auth state
        if (_authenticationStateChanged is false)
            return _currentAuthenticationState;

        CurrentUserDto? currentUser;
        try
        {
            currentUser = await _authClient.GetCurrentUserAsync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Exception occurred while getting authentication state");
            return _currentAuthenticationState;
        }

        var currentClaimsPrincipal = currentUser?.ToClaimsPrincipal() ?? new ClaimsPrincipal();

        _currentAuthenticationState = new AuthenticationState(currentClaimsPrincipal);

        // Reset auth state changed to false, now that we've updated it
        _authenticationStateChanged = false;

        return _currentAuthenticationState;
    }

    public async Task<bool> LoginAsync(string? username, string? password)
    {
        bool loggedIn = await _authClient.LoginAsync(username, password);
        if (loggedIn)
        {
            _authenticationStateChanged = true;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        return loggedIn;
    }

    public async Task<bool> CreateUserAsync(string? username, string? password)
    {
        bool userCreated = await _authClient.CreateUserAsync(username, password);
        if (userCreated)
        {
            _authenticationStateChanged = true;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        return userCreated;
    }

    public async Task<bool> LogoutAsync()
    {
        bool loggedOut = await _authClient.LogoutAsync();
        if (loggedOut)
        {
            _authenticationStateChanged = true;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        return loggedOut;
    }


    public async Task<bool> DeleteUserAsync()
    {
        bool userDeleted = await _userClient.DeleteUserAsync();
        if (userDeleted)
        {
            _authenticationStateChanged = true;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        return userDeleted;
    }
}
