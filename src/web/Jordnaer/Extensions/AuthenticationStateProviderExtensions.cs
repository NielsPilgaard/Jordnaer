using Jordnaer.Shared;
using Microsoft.AspNetCore.Components.Authorization;

namespace Jordnaer.Extensions;

public static class AuthenticationStateProviderExtensions
{
	public static async Task<string?> GetCurrentUserId(this AuthenticationStateProvider authenticationStateProvider)
	{
		var authState = await authenticationStateProvider.GetAuthenticationStateAsync();

		return authState.User.Identity?.IsAuthenticated is true
				   ? authState.User.GetId()
				   : null;
	}
}