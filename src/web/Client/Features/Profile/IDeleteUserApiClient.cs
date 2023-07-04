using Refit;

namespace Jordnaer.Client.Features.Profile;


public interface IDeleteUserApiClient
{
    /// <summary>
    /// Initiates the delete user procedure. This generates a unique user token that is sent to the user through their email.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    [Get("/api/delete-user/{id}")]
    Task<IApiResponse> InitiateDeleteUserAsync(string id);

    /// <summary>
    /// Deletes the user.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    [Delete("/api/delete-user/{id}")]
    Task<IApiResponse> DeleteUserAsync(string id);
}
