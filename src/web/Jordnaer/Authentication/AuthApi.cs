using Jordnaer.Authorization;
using Jordnaer.Extensions;
using Jordnaer.Features.Profile;
using Jordnaer.Shared;
using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Claim = System.Security.Claims.Claim;

namespace Jordnaer.Authentication;

public static class AuthApi
{
	public static RouteGroupBuilder MapAuthentication(this IEndpointRouteBuilder routes)
	{
		var group = routes.MapGroup("api/auth");

		group.RequireAuthRateLimit();

		group.MapPost("register", async ([FromBody] UserInfo userInfo, [FromServices] IUserService userService) =>
		{
			var userId = await userService.CreateUserAsync(userInfo);

			return userId is not null
				? SignIn(userId, userInfo, new AuthenticationProperties { RedirectUri = "/first-login" })
				: Results.Unauthorized();
		});

		group.MapPost("login", async ([FromBody] UserInfo userInfo, [FromServices] IUserService userService) =>
		{
			// Check whether the user exists
			var userId = await userService.IsLoginValidAsync(userInfo);

			return userId is not null
				? SignIn(userId, userInfo, new AuthenticationProperties { RedirectUri = "/" })
				: Results.Unauthorized();
		});

		group.MapPost("logout", async context => await context.SignOutFromAllAccountsAsync()).RequireAuthorization();

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
			=> currentUser?.Principal?.ToCurrentUser());

		group.MapGet("signin/{provider}", async ([FromRoute] string provider, [FromServices] IUserService userService, [FromServices] IMediator mediator, HttpContext context) =>
		{
			// Grab the login information from the external login dance
			var result = await context.AuthenticateAsync(AuthConstants.ExternalScheme);
			if (!result.Succeeded)
			{
				return Results.Unauthorized();
			}

			var id = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

			var name = (result.Principal.FindFirstValue(ClaimTypes.Email) ?? result.Principal.Identity?.Name)!;

			var getOrCreateUserResult = await userService.GetOrCreateUserAsync(
				provider,
				new ExternalUserInfo { Email = name, ProviderKey = id });

			if (getOrCreateUserResult is
				GetOrCreateUserResult.FailedToCreateUser or
				GetOrCreateUserResult.FailedToAddLogin)
			{
				return Results.Forbid();
			}

			var accessToken = result.Properties?.GetTokenValue("access_token");
			if (accessToken is not null)
			{
				await mediator.Publish(new AccessTokenAcquired(id,
					provider,
					id,
					accessToken));
			}

			// Delete the external cookie
			await context.SignOutAsync(AuthConstants.ExternalScheme);

			// If the user was just created, redirect them to the first-login page
			var redirectUri = getOrCreateUserResult is GetOrCreateUserResult.UserCreated
								  ? "/first-login"
								  : "/";

			var properties = new AuthenticationProperties { RedirectUri = redirectUri };

			return SignIn(provider, result.Principal.Claims, properties);
		});

		return group;
	}

	private static IResult SignIn(string userId, UserInfo userInfo, AuthenticationProperties properties)
		=> SignIn(providerName: null,
				claims: new[]
				{
					new Claim(ClaimTypes.NameIdentifier, userId),
					new Claim(ClaimTypes.Email, userInfo.Email),
					new Claim(ClaimTypes.Name, userInfo.Email)
				},
				properties);

	private static IResult SignIn(string? providerName,
		IEnumerable<Claim> claims, AuthenticationProperties? properties = null)
	{
		var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
		identity.AddClaims(claims);

		properties ??= new AuthenticationProperties();

		// Store the external provider name so we can do remote sign out
		if (providerName is not null)
		{
			properties.SetExternalProvider(providerName);
		}

		properties.IsPersistent = true;

		return Results.SignIn(new ClaimsPrincipal(identity),
			properties: properties,
			authenticationScheme: CookieAuthenticationDefaults.AuthenticationScheme);
	}
}
