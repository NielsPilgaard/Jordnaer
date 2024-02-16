using System.Security.Claims;
using Jordnaer.Shared;

namespace Jordnaer.Features.Authentication;

public class CurrentUser
{
	public ClaimsPrincipal User { get; internal set; } = new(new ClaimsPrincipal());

	/// <summary>
	/// This can be null if the user is not logged in, so only use it in pages that require authentication.
	/// </summary>
	public string Id => User.GetId();

	public string? Cookie { get; internal set; }
}