using Jordnaer.Shared;
using Refit;

namespace Jordnaer.Features.Profile;

public interface IProfileClient
{
	/// <summary>
	/// Gets the user profile that matches <paramref name="userName"/>.
	/// <para>
	/// If no such user profile exists, <c>404 Not Found</c> is returned.
	/// </para>
	/// </summary>
	/// <param name="userName">Name of the user.</param>
	/// <returns></returns>
	[Get("/api/profiles/{username}")]
	Task<IApiResponse<ProfileDto>> GetUserProfile(string userName);

	/// <summary>
	/// Updates the profile of the user that is currently logged in.
	/// </summary>
	/// <param name="userProfile">The user profile.</param>
	[Put("/api/profiles")]
	Task<IApiResponse> UpdateUserProfile(UserProfile userProfile);
}
