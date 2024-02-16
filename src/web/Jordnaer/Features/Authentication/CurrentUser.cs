using System.Security.Claims;
using Jordnaer.Shared;

namespace Jordnaer.Features.Authentication;

public class CurrentUser
{
	public ClaimsPrincipal User { get; internal set; } = new(new ClaimsPrincipal());

	public string Id => User.GetId();

	public string? Cookie { get; internal set; }
}