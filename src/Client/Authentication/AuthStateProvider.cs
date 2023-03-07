using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using RemindMeApp.Shared;

namespace RemindMeApp.Client.Authentication;

public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly AuthClient _client;
    private readonly ILogger<AuthStateProvider> _logger;
    private AuthenticationState _currentAuthenticationState;
    private bool _authenticationStateChanged = true;

    public AuthStateProvider(AuthClient client, ILogger<AuthStateProvider> logger)
    {
        _client = client;
        _logger = logger;
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
            currentUser = await _client.GetCurrentUserAsync();
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
        bool response = await _client.LoginAsync(username, password);

        if (response)
        {
            _authenticationStateChanged = true;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        return response;
    }

    public async Task<bool> CreateUserAsync(string? username, string? password)
    {
        bool response = await _client.CreateUserAsync(username, password);

        if (response)
        {
            _authenticationStateChanged = true;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        return response;
    }

    public async Task<bool> LogoutAsync()
    {
        bool response = await _client.LogoutAsync();
        if (response)
        {
            _authenticationStateChanged = true;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        return response;
    }
}
