using System.Net.Http.Headers;
using Jordnaer.Extensions;
using Jordnaer.Shared;

namespace Jordnaer.Features.Images;

public sealed class GooglePictureDownloader(
	IHttpClientFactory httpClientFactory,
	ILogger<MicrosoftPictureDownloader> logger,
	IImageService imageService)
	: IExternalProviderPictureDownloader
{
	private const string GoogleUrl = "https://www.googleapis.com/oauth2/v1/userinfo?alt=json";

	public async ValueTask<string?> GetProfilePictureUrlAsync(AccessTokenAcquired accessTokenAcquired, ExternalProvider externalProvider,
		CancellationToken cancellationToken = default)
	{
		var client = httpClientFactory.CreateClient(HttpClients.Default);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessTokenAcquired.AccessToken);

		var response = await client.GetAsync(GoogleUrl, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			logger.LogError("Failed to retrieve the Google profile picture for user with id {userId}. " +
							"{statusCode}: {reasonPhrase}", accessTokenAcquired.ProviderKey, response.StatusCode, response.ReasonPhrase);
			return null;
		}

		var profilePictureResponse = await response
										   .Content
										   .ReadFromJsonAsync<GooglePictureResponse>(cancellationToken);

		if (profilePictureResponse?.Picture is null)
		{
			logger.LogWarning("Failed to retrieve the Google profile picture for user with id {userId}." +
							  "The response was successful, but was not in the expected format." +
							  "Received JSON: {response}", accessTokenAcquired.ProviderKey,
							  await response.Content.ReadAsStringAsync(cancellationToken));
			return null;
		}

		var imageStream = await imageService.GetImageStreamFromUrlAsync(
							  profilePictureResponse.Picture,
							  cancellationToken);

		if (imageStream is null)
		{
			return ProfileConstants.Default_Profile_Picture;
		}

		await using var resizedImage = await imageService.ResizeImageAsync(imageStream, cancellationToken);

		var imageUrl = await imageService.UploadImageAsync(accessTokenAcquired.UserId,
														   ProfileImageService.UserProfilePicturesContainerName,
														   resizedImage,
														   cancellationToken);

		return imageUrl;
	}
}