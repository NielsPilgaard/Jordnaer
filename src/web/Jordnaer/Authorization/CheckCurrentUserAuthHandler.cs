using Jordnaer.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Jordnaer.Authorization;

public static class AuthorizationHandlerExtensions
{
	public static AuthorizationBuilder AddCurrentUserHandler(this AuthorizationBuilder builder)
	{
		builder.Services.AddScoped<IAuthorizationHandler, CheckCurrentUserAuthHandler>();
		return builder;
	}

	// Adds the current user requirement that will activate our authorization handler
	public static AuthorizationPolicyBuilder RequireCurrentUser(this AuthorizationPolicyBuilder builder) =>
		builder.RequireAuthenticatedUser().AddRequirements(new CheckCurrentUserRequirement());

	public static RouteHandlerBuilder RequireCurrentUser(this RouteHandlerBuilder builder) =>
		builder.RequireAuthorization(policyBuilder => policyBuilder.RequireCurrentUser());

	private class CheckCurrentUserRequirement : IAuthorizationRequirement;

	// This authorization handler verifies that the user exists even if there's
	// a valid token
	private class CheckCurrentUserAuthHandler : AuthorizationHandler<CheckCurrentUserRequirement>
	{
		private readonly CurrentUser _currentUser;
		private readonly UserManager<ApplicationUser> _userManager;

		public CheckCurrentUserAuthHandler(CurrentUser currentUser, UserManager<ApplicationUser> userManager)
		{
			_currentUser = currentUser;
			_userManager = userManager;
		}

		protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
			CheckCurrentUserRequirement requirement)
		{
			if (_currentUser.User is null)
			{
				return;
			}

			if (!_userManager.SupportsUserLockout)
			{
				// User cannot be locked out
				context.Succeed(requirement);
			}

			var userIsLockedOut = await _userManager.IsLockedOutAsync(_currentUser.User);
			if (!userIsLockedOut)
			{
				context.Succeed(requirement);
			}
		}
	}
}
