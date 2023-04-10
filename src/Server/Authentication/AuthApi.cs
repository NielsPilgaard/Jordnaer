using System.Security.Claims;
using Jordnaer.Server.Authorization;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Jordnaer.Server.Authentication;

public static class AuthApi
{
    public static RouteGroupBuilder MapAuthentication(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/auth");

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
            bool loginIsValid = await userService.IsLoginValidAsync(userInfo);

            return loginIsValid
                ? SignIn(userInfo)
                : Results.Unauthorized();
        });

        group.MapPost("logout", async (HttpContext context, [FromServices] CurrentUser? currentUser) =>
        {
            if (currentUser?.User is null)
            {
                return Results.Unauthorized();
            }

            await context.SignOutAsync();

            return Results.Ok();
        });

        // External login
        group.MapGet("login/{provider}", ([FromRoute] string provider) =>
        {
            // Trigger the external login flow by issuing a challenge with the provider name.
            // This name maps to the registered authentication scheme names in AuthExtensions.cs
            return Results.Challenge(
                properties: new AuthenticationProperties { RedirectUri = $"/api/auth/signin/{provider}" },
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
                await SignIn(provider, result.Principal.Claims, result.Properties.GetTokens()).ExecuteAsync(context);
            }

            // Delete the external cookie
            await context.SignOutAsync(AuthConstants.ExternalScheme);

            return Results.Redirect("/");
        });

        return group;
    }

    private static IResult SignIn(UserInfo userInfo)
        => SignIn(providerName: null,
                claims: new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userInfo.Email),
                    new Claim(ClaimTypes.Email, userInfo.Email),
                    new Claim(ClaimTypes.Name, userInfo.Email)
                },
                authTokens: Enumerable.Empty<AuthenticationToken>());

    private static IResult SignIn(string? providerName,
        IEnumerable<Claim> claims,
        IEnumerable<AuthenticationToken> authTokens)
    {
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaims(claims);

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

        properties.IsPersistent = true;

        return Results.SignIn(new ClaimsPrincipal(identity),
            properties: properties,
            authenticationScheme: CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
