using Jordnaer.Server.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Jordnaer.Server.Extensions;

public static class HttpContextExtensions
{
    public static async Task SignOutFromAllAccountsAsync(this HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        var result = await context.AuthenticateAsync();
        if (result.Properties?.GetExternalProvider() is not null)
        {
            await context.SignOutAsync(AuthConstants.ExternalScheme, new AuthenticationProperties { RedirectUri = "/" });
        }
    }
}
