using Jordnaer.Shared;
using Refit;

namespace Jordnaer.Client.Features.Profile;

public interface IImageClient
{
    /// <summary>
    /// Sets the child profile's picture.
    /// </summary>
    /// <param name="setChildProfilePicture">The child profile to set the picture for, and the image in byte array form.</param>
    /// <returns>The absolute uri to the new profile picture.</returns>
    [Put("/api/images/child-profile")]
    Task<IApiResponse<string>> SetChildProfilePicture([Body] SetChildProfilePicture setChildProfilePicture);

    /// <summary>
    /// Sets the user profile's picture.
    /// </summary>
    /// <param name="setUserProfilePicture">The user profile to set the picture for, and the image in byte array form.</param>
    /// <returns>The absolute uri to the new profile picture.</returns>
    [Put("/api/images/user-profile")]
    Task<IApiResponse<string>> SetUserProfilePicture([Body] SetUserProfilePicture setUserProfilePicture);
}
