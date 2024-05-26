using System.Net;
using Jordnaer.Shared;
using System.Security.Claims;

namespace Jordnaer.Features.Authentication;

public class CurrentUser
{
	public ClaimsPrincipal User { get; internal set; } = new(new ClaimsPrincipal());

	/// <summary>
	/// This is null if the user is not logged in, so only use it in pages that require authentication.
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

	/// <summary>
	/// Due to this website having multiple host names,
	/// the CurrentUser can be on a range of different urls.
	/// The current user's url is saved on Blazor circuit lifetime events.
	/// </summary>
	public string Url { get; internal set; } = "https://mini-moeder.dk";
}