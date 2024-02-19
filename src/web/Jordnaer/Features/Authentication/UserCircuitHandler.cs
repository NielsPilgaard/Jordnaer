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
	IHttpContextAccessor httpContextAccessor)
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
		if (httpContextAccessor.HttpContext is null)
		{
			logger.LogWarning("No HttpContext is associated with Circuit {CircuitId}", circuit.Id);
			return Task.CompletedTask;
		}

		if (!httpContextAccessor.HttpContext.Request.Cookies.TryGetValue(AuthenticationConstants.CookieName, out var cookie))
		{
			logger.LogError("Failed to get cookie by name '{CookieName}'", AuthenticationConstants.CookieName);
			return Task.CompletedTask;
		}

		logger.LogInformation("Successfully set cookie for User {UserId}", currentUser.Id);

		currentUser.Cookie = cookie;

		return Task.CompletedTask;
	}

	public void Dispose()
	{
		authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationChanged;
		profileCache.ProfileChanged -= OnProfileChanged;
	}
}