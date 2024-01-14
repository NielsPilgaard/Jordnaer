using Jordnaer.Database;
using Microsoft.AspNetCore.Identity;

namespace Jordnaer.Components.Account;

internal sealed class IdentityUserAccessor(UserManager<ApplicationUser> userManager, IdentityRedirectManager redirectManager)
{
	public async Task<ApplicationUser> GetRequiredUserAsync(HttpContext context)
	{
		var user = await userManager.GetUserAsync(context.User);

		if (user is null)
		{
			redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Fejl: Kunne ikke indlæse bruger med ID '{userManager.GetUserId(context.User)}'.", context);
		}

		return user;
	}
}