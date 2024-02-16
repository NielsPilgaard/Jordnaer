using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Jordnaer.Features.Authentication;

internal sealed class UserCircuitHandler(
	AuthenticationStateProvider authenticationStateProvider,
	UserService userService,
	ILogger<UserCircuitHandler> logger)
	: CircuitHandler, IDisposable
{
	public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
	{
		authenticationStateProvider.AuthenticationStateChanged += AuthenticationChanged;

		return base.OnCircuitOpenedAsync(circuit, cancellationToken);
	}

	private void AuthenticationChanged(Task<AuthenticationState> authenticationChanged)
	{
		_ = UpdateAuthentication(authenticationChanged);

		return;

		async Task UpdateAuthentication(Task<AuthenticationState> task)
		{
			try
			{
				var state = await task;
				userService.SetUser(state.User);
			}
			catch (Exception exception)
			{
				logger.LogError(exception, "Failed to update authentication state");
			}
		}
	}

	public override async Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
	{
		var state = await authenticationStateProvider.GetAuthenticationStateAsync();
		userService.SetUser(state.User);
	}

	public void Dispose() => authenticationStateProvider.AuthenticationStateChanged -= AuthenticationChanged;
}