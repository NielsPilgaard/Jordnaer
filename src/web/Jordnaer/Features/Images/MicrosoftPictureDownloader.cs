using System.Net.Http.Headers;
using Jordnaer.Extensions;

namespace Jordnaer.Features.Images;

public sealed class MicrosoftPictureDownloader(
	IHttpClientFactory httpClientFactory,
	ILogger<MicrosoftPictureDownloader> logger,
	IImageService imageService)
	: IExternalProviderPictureDownloader
{
	public async ValueTask<string?> GetProfilePictureUrlAsync(AccessTokenAcquired accessTokenAcquired, ExternalProvider externalProvider,
		CancellationToken cancellationToken = default)
	{
		if (externalProvider != ExternalProvider.Microsoft)
		{
			return null;
		}

		const string microsoftUrl = "https://graph.microsoft.com/v1.0/me/photo/$value";

		var client = httpClientFactory.CreateClient(HttpClients.Default);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessTokenAcquired.AccessToken);

		var response = await client.GetAsync(microsoftUrl, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			logger.LogError("Failed to retrieve the Microsoft profile picture for user with id {userId}. " +
							"{statusCode}: {reasonPhrase}",
							accessTokenAcquired.ProviderKey,
							response.StatusCode,
							response.ReasonPhrase);

			return null;
		}

		await using var imageAsStream = await response.Content.ReadAsStreamAsync(cancellationToken);

		var imageUrl = await imageService.UploadImageAsync(
						   accessTokenAcquired.UserId,
						   ProfileImageService.UserProfilePicturesContainerName,
						   imageAsStream,
						   cancellationToken);

		return imageUrl;
	}
}