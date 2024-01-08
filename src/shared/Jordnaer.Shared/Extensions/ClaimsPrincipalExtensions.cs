using System.Security.Claims;

namespace Jordnaer.Shared;

public static class ClaimsPrincipalExtensions
{
	public static UserProfile ToUserProfile(this ClaimsPrincipal principal, string userId)
	{
		var user = new UserProfile { Id = userId };

		var fullName = principal.FindFirstValue(ClaimTypes.Name);
		if (fullName is not null)
		{
			var splitFullName = fullName.Split(" ");
			user.FirstName = splitFullName[0];

			if (splitFullName.Length > 1)
			{
				user.LastName = string.Join(' ', splitFullName[1..]);
			}

			user.UserName = fullName.Replace(" ", "");
		}
		else
		{
			var firstName = principal.FindFirstValue(ClaimTypes.GivenName);
			var lastName = principal.FindFirstValue(ClaimTypes.Surname);
			user.FirstName = firstName;
			user.LastName = lastName;
			user.UserName = firstName + lastName?.Replace(" ", "");
		}

		// TODO: Can we get this information from external auth?

		//principal.FindFirst(ClaimTypes.DateOfBirth);
		//principal.FindFirst(ClaimTypes.PostalCode);
		//principal.FindFirst(ClaimTypes.Gender);

		return user;
	}
	private static string? FindFirstValue(this ClaimsPrincipal principal, string type) => principal.FindFirst(type)?.Value;

	public static string GetId(this ClaimsPrincipal principal) => principal.FindFirst(ClaimTypes.NameIdentifier)!.Value;
}
