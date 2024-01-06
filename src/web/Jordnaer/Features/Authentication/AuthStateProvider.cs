using Jordnaer.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Jordnaer.Features.Authentication;

public class AuthStateProvider : AuthenticationStateProvider
{
	private readonly IAuthClient _authClient;
	private readonly ILogger<AuthStateProvider> _logger;
	private AuthenticationState _currentAuthenticationState;
	private bool _authenticationStateChanged = true;

	public AuthStateProvider(IAuthClient authClient, ILogger<AuthStateProvider> logger)
	{
		_authClient = authClient;
		_logger = logger;
		_currentAuthenticationState = new AuthenticationState(new ClaimsPrincipal());
	}

	public override async Task<AuthenticationState> GetAuthenticationStateAsync()
	{
		// If auth state is unchanged, return cached auth state
		if (_authenticationStateChanged is false)
			return _currentAuthenticationState;

		CurrentUserDto? currentUser;
		var response = await _authClient.GetCurrentUserAsync();
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
		var response = await _authClient.LoginAsync(userInfo);
		if (response is { IsSuccessStatusCode: true, Content: true })
		{
			_authenticationStateChanged = true;
			NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
		}

		return false;
	}

	public async Task<bool> CreateUserAsync(UserInfo userInfo)
	{
		var response = await _authClient.CreateUserAsync(userInfo);
		if (response is { IsSuccessStatusCode: true, Content: true })
		{
			_authenticationStateChanged = true;
			NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
		}

		return false;
	}

	public async Task<bool> LogoutAsync()
	{
		var response = await _authClient.LogoutAsync();
		if (response.IsSuccessStatusCode)
		{
			_authenticationStateChanged = true;
			NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
		}

		return false;
	}
}
