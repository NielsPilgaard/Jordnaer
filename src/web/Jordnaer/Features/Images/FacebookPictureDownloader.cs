using Jordnaer.Extensions;
using Jordnaer.Shared;

namespace Jordnaer.Features.Images;

public sealed class FacebookPictureDownloader(
	IHttpClientFactory httpClientFactory,
	ILogger<MicrosoftPictureDownloader> logger,
	IImageService imageService)
	: IExternalProviderPictureDownloader
{
	public async ValueTask<string?> GetProfilePictureUrlAsync(AccessTokenAcquired accessTokenAcquired, ExternalProvider externalProvider,
		CancellationToken cancellationToken = default)
	{
		if (externalProvider != ExternalProvider.Facebook)
		{
			return null;
		}

		var client = httpClientFactory.CreateClient(HttpClients.Default);
		var facebookUrl = $"https://graph.facebook.com/v13.0/{accessTokenAcquired.ProviderKey}/picture?" +
						  $"type=large&" +
						  $"redirect=false&" +
						  $"access_token={accessTokenAcquired.AccessToken}";

		var response = await client.GetAsync(facebookUrl, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			logger.LogError("Failed to retrieve the Facebook profile picture for user with id {userId}. " +
							"Response: {@response}", accessTokenAcquired.ProviderKey,
							await response.Content.ReadAsStringAsync(cancellationToken));
			return null;
		}

		var profilePictureResponse =
			await response.Content.ReadFromJsonAsync<FacebookProfilePictureResponse>(
				cancellationToken: cancellationToken);

		if (profilePictureResponse?.Data?.Url is null)
		{
			logger.LogWarning("Failed to retrieve the Facebook profile picture for user with id {userId}." +
							  "The response was successful, but was not in the expected format." +
							  "Received JSON: {response}", accessTokenAcquired.ProviderKey,
							  await response.Content.ReadAsStringAsync(cancellationToken));
			return null;
		}

		var imageStream = await imageService.GetImageStreamFromUrlAsync(
							  profilePictureResponse.Data.Url,
							  cancellationToken);

		if (imageStream is null)
		{
			return ProfileConstants.Default_Profile_Picture;
		}

		var resizedImage = await imageService.ResizeImageAsync(imageStream, cancellationToken);

		var imageUrl =
			await imageService.UploadImageAsync(accessTokenAcquired.UserId,
												ProfileImageService.UserProfilePicturesContainerName,
												resizedImage,
												cancellationToken);

		await resizedImage.DisposeAsync();

		return imageUrl;
	}
}