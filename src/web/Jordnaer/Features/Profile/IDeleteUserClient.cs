using Refit;

namespace Jordnaer.Features.Profile;


public interface IDeleteUserClient
{
	/// <summary>
	/// Initiates the delete user procedure for the current user.
	/// <para>This generates a user token that is sent to the user through their email.</para>
	/// </summary>
	[Get("/api/delete-user")]
	Task<IApiResponse> InitiateDeleteUserAsync();

	/// <summary>
	/// Deletes the current user.
	/// </summary>
	/// <param name="token">The deletion request token.</param>
	[Delete("/api/delete-user")]
	Task<IApiResponse> DeleteUserAsync(string token);

	/// <summary>
	/// Verifies that the token supplied is valid for the current user.
	/// </summary>
	/// <param name="token"></param>
	/// <returns></returns>
	[Get("/api/delete-user/verify-token")]
	Task<IApiResponse> VerifyTokenAsync(string token);
}
