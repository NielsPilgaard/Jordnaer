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
	ILogger<UserCircuitHandler> logger)
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

	public void Dispose()
	{
		authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationChanged;
		profileCache.ProfileChanged -= OnProfileChanged;
	}
}