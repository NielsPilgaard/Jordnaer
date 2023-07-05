using Refit;

namespace Jordnaer.Client.Features.Profile;


public interface IDeleteUserApiClient
{
    /// <summary>
    /// Initiates the delete user procedure. This generates a unique user token that is sent to the user through their email.
    /// </summary>
    [Get("/api/delete-user")]
    Task<IApiResponse> InitiateDeleteUserAsync();

    /// <summary>
    /// Deletes the user.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    [Delete("/api/delete-user")]
    Task<IApiResponse> DeleteUserAsync(string token);
}
