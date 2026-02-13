using System.Net;
using Jordnaer.Shared;
using System.Security.Claims;
using Jordnaer.Database;

namespace Jordnaer.Features.Authentication;

public class CurrentUser
{
	public ClaimsPrincipal User { get; internal set; } = new(new ClaimsPrincipal());

	/// <summary>
	/// This Id is the same as <see cref="UserProfile.Id"/> AND <see cref="ApplicationUser.Id"/>.
	/// <para>This is null if the user is not logged in, so only use it in pages that require authentication.</para>
	/// </summary>
	public string? Id => User.GetId();

	/// <summary>
	/// This is null if the user is not logged in, so only use it in pages that require authentication.
	/// </summary>
	public CookieContainer? CookieContainer { get; internal set; }

	/// <summary>
	/// This is null if the user is not logged in, so only use it in pages that require authentication.
	/// </summary>
	public UserProfile? UserProfile { get; internal set; }
}