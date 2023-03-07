using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RemindMeApp.Server.Authorization;
using RemindMeApp.Shared;

namespace RemindMeApp.Server.Authentication;

public static class AuthApi
{
    public static RouteGroupBuilder MapAuthentication(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/authentication");

        group.MapPost("register", async ([FromBody] UserInfo userInfo, [FromServices] IUserService userService) =>
        {
            bool userCreated = await userService.CreateUserAsync(userInfo);

            return userCreated
                ? SignIn(userInfo)
                : Results.Unauthorized();
        });

        group.MapPost("login", async ([FromBody] UserInfo userInfo, [FromServices] IUserService userService) =>
        {
            // Check whether the user exists
            bool loginIsValid = await userService.LoginIsValid(userInfo);

            return loginIsValid
                ? SignIn(userInfo)
                : Results.Unauthorized();
        });

        group.MapPost("logout", async context =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // TODO: Support remote logout
            // If this is an external login then use it
            var result = await context.AuthenticateAsync();
            if (result.Properties?.GetExternalProvider() is { } providerName)
            {
                await context.SignOutAsync(providerName, new AuthenticationProperties { RedirectUri = "/" });
            }
        })
        .RequireAuthorization();

        // External login
        group.MapGet("login/{provider}", ([FromRoute] string provider) =>
        {
            // Trigger the external login flow by issuing a challenge with the provider name.
            // This name maps to the registered authentication scheme names in AuthExtensions.cs
            return Results.Challenge(
                properties: new AuthenticationProperties { RedirectUri = $"/authentication/signin/{provider}" },
                authenticationSchemes: new[] { provider });
        });

        group.MapGet("current-user", ([FromServices] CurrentUser? currentUser)
            => currentUser is null
                ? null
                : CurrentUserDto.From(currentUser.Principal));

        group.MapGet("signin/{provider}", async ([FromRoute] string provider, [FromServices] IUserService userService, HttpContext context) =>
        {
            // Grab the login information from the external login dance
            var result = await context.AuthenticateAsync(AuthConstants.ExternalScheme);

            if (result.Succeeded)
            {
                var principal = result.Principal;

                string id = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

                // TODO: We should have the user pick a user name to complete the external login dance
                // for now we'll prefer the email address
                string name = (principal.FindFirstValue(ClaimTypes.Email) ?? principal.Identity?.Name)!;

                await SignIn(id, name, provider, result.Properties.GetTokens()).ExecuteAsync(context);
            }

            // Delete the external cookie
            await context.SignOutAsync(AuthConstants.ExternalScheme);

            // TODO: Handle the failure somehow

            return Results.Redirect("/");
        });

        return group;
    }

    private static IResult SignIn(UserInfo userInfo)
        => SignIn(userInfo.Username,
                userInfo.Username,
                providerName: null,
                authTokens: Enumerable.Empty<AuthenticationToken>());

    private static IResult SignIn(string userId, string userName, string? providerName, IEnumerable<AuthenticationToken> authTokens)
    {
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
        identity.AddClaim(new Claim(ClaimTypes.Name, userName));

        var properties = new AuthenticationProperties();

        // Store the external provider name so we can do remote sign out
        if (providerName is not null)
        {
            properties.SetExternalProvider(providerName);
        }

        bool hasAuthToken = authTokens.Any();
        if (hasAuthToken)
        {
            properties.SetHasExternalToken(true);
        }

        return Results.SignIn(new ClaimsPrincipal(identity),
            properties: properties,
            authenticationScheme: CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
