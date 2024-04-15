using Jordnaer.Extensions;
using Jordnaer.Features.Profile;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Jordnaer.Features.Authentication;

internal sealed class UserCircuitHandler(
	AuthenticationStateProvider authenticationStateProvider,
	CurrentUser currentUser,
	IProfileCache profileCache,
	ILogger<UserCircuitHandler> logger,
	IHttpContextAccessor httpContextAccessor,
	CookieContainerFactory cookieContainerFactory)
	: CircuitHandler, IDisposable
{
	public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
	{
		authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationChanged;
		profileCache.ProfileChanged += OnProfileChanged;

		var state = await authenticationStateProvider.GetAuthenticationStateAsync();
		currentUser.User = state.User;
		currentUser.UserProfile = await profileCache.GetProfileAsync(cancellationToken);

		await base.OnCircuitOpenedAsync(circuit, cancellationToken);
	}

	private void OnProfileChanged(object? sender, UserProfile userProfile)
		=> currentUser.UserProfile = userProfile;

	private void OnAuthenticationChanged(Task<AuthenticationState> authenticationChanged)
	{
		_ = UpdateAuthentication(authenticationChanged);

		return;

		async Task UpdateAuthentication(Task<AuthenticationState> task)
		{
			try
			{
				var state = await task;
				currentUser.User = state.User;
			}
			catch (Exception exception)
			{
				logger.LogException(exception);
			}
		}
	}

	public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
	{
		if (currentUser.CookieContainer is not null)
		{
			logger.LogDebug("CurrentUser already has a Cookie Container, returning.");
			return Task.CompletedTask;
		}

		if (httpContextAccessor.HttpContext is null)
		{
			logger.LogWarning("No HttpContext is associated with Circuit {CircuitId}", circuit.Id);
			return Task.CompletedTask;
		}

		if (!httpContextAccessor.HttpContext.Request.Cookies
								.TryGetValue(AuthenticationConstants.CookieName, out var cookie))
		{
			if (currentUser.Id is not null)
			{
				logger.LogError("Failed to get cookie by name '{CookieName}' by logged in User {UserId}", AuthenticationConstants.CookieName, currentUser.Id);
			}

			// User is not yet logged in, return early.
			return Task.CompletedTask;
		}

		var domain = httpContextAccessor.HttpContext.Request.Host.Host;

		var cookieContainer = cookieContainerFactory.Create(cookie, domain);

		currentUser.CookieContainer = cookieContainer;

		logger.LogDebug("Successfully set cookie for User {UserId}", currentUser.Id);

		return Task.CompletedTask;
	}

	public void Dispose()
	{
		authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationChanged;
		profileCache.ProfileChanged -= OnProfileChanged;
	}
}