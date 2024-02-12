using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Shared;
using Mediator;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace Jordnaer.Features.Profile;

public readonly struct AccessTokenAcquired : INotification
{
	public readonly string UserId;
	public readonly string ProviderKey;
	public readonly string LoginProvider;
	public readonly string AccessToken;

	public AccessTokenAcquired(string userId, string providerKey, string loginProvider, string accessToken)
	{
		Debug.Assert(!string.IsNullOrEmpty(userId));
		Debug.Assert(!string.IsNullOrEmpty(providerKey));
		Debug.Assert(!string.IsNullOrEmpty(loginProvider));
		Debug.Assert(!string.IsNullOrEmpty(accessToken));

		UserId = userId;
		ProviderKey = providerKey;
		LoginProvider = loginProvider;
		AccessToken = accessToken;
	}
}

public class ExternalProfilePictureDownloader : INotificationHandler<AccessTokenAcquired>
{
	private readonly JordnaerDbContext _context;
	private readonly ILogger<ExternalProfilePictureDownloader> _logger;
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly IImageService _imageService;

	public ExternalProfilePictureDownloader(
		JordnaerDbContext context,
		ILogger<ExternalProfilePictureDownloader> logger,
		IHttpClientFactory httpClientFactory,
		IImageService imageService)
	{
		_context = context;
		_logger = logger;
		_httpClientFactory = httpClientFactory;
		_imageService = imageService;
	}

	public async ValueTask Handle(AccessTokenAcquired notification, CancellationToken cancellationToken)
	{
		if (!Enum.TryParse<SupportedAuthProviders>(notification.LoginProvider, out var provider))
		{
			_logger.LogError("Failed to parse provider {provider} to SupportedAuthProviders enum. " +
							 "Valid values: {supportedAuthProviders}",
				notification.LoginProvider, string.Join(", ", Enum.GetValues<SupportedAuthProviders>()));
			return;
		}

		var user = await _context.UserProfiles.FindAsync([notification.UserId], cancellationToken);
		if (user is null)
		{
			_logger.LogWarning("User has not been created yet, we cannot set their profile picture. " +
							 "UserId: {UserId} LoginProvider: {Provider}", notification.UserId, notification.LoginProvider);
			return;
		}

		var profilePictureUrl = provider switch
		{
			SupportedAuthProviders.Facebook => await GetFacebookProfilePictureUrlAsync(
				notification,
				cancellationToken),

			SupportedAuthProviders.Google => await GetGoogleProfilePictureUrlAsync(
				notification,
				cancellationToken),

			SupportedAuthProviders.Microsoft => await GetMicrosoftProfilePictureUrlAsync(
				notification,
				cancellationToken),

			_ => throw new UnreachableException($"Encountered unexpected enum value {provider} in enum {nameof(SupportedAuthProviders)}")
		};

		if (profilePictureUrl is null)
		{
			_logger.LogWarning("Failed to retrieve the {provider} profile picture for user with id {userId}", provider.ToString(), user.Id);

			return;
		}

		user.ProfilePictureUrl = profilePictureUrl;

		_context.UserProfiles.Update(user);

		await _context.SaveChangesAsync(cancellationToken);
	}

	public async Task<string?> GetFacebookProfilePictureUrlAsync(AccessTokenAcquired notification,
		CancellationToken cancellationToken)
	{
		var client = _httpClientFactory.CreateClient(HttpClients.External);
		var facebookUrl = $"https://graph.facebook.com/v13.0/{notification.ProviderKey}/picture?" +
						  $"type=large&" +
						  $"redirect=false&" +
						  $"access_token={notification.AccessToken}";

		var response = await client.GetAsync(facebookUrl, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			_logger.LogError("Failed to retrieve the Facebook profile picture for user with id {userId}. " +
							 "Response: {@response}", notification.ProviderKey, await response.Content.ReadAsStringAsync(cancellationToken));
			return null;
		}

		var profilePictureResponse =
			await response.Content.ReadFromJsonAsync<FacebookProfilePictureResponse>(
				cancellationToken: cancellationToken);

		if (profilePictureResponse?.Data?.Url is null)
		{
			_logger.LogWarning("Failed to retrieve the Facebook profile picture for user with id {userId}." +
							   "The response was successful, but was not in the expected format." +
							   "Received JSON: {response}", notification.ProviderKey,
				await response.Content.ReadAsStringAsync(cancellationToken));
			return null;
		}

		var imageStream = await GetImageStreamFromUrlAsync(
			profilePictureResponse.Data.Url,
			cancellationToken);

		if (imageStream is null)
		{
			return ProfileConstants.Default_Profile_Picture;
		}

		var resizedImage = await ResizeImageAsync(imageStream, cancellationToken);

		var imageUrl =
			await _imageService.UploadImageAsync(notification.UserId,
												 ProfileImageService.UserProfilePicturesContainerName,
												 resizedImage,
												 cancellationToken);

		await resizedImage.DisposeAsync();

		return imageUrl;
	}

	private async Task<string?> GetGoogleProfilePictureUrlAsync(AccessTokenAcquired notification,
		CancellationToken cancellationToken)
	{
		const string googleUrl = "https://www.googleapis.com/oauth2/v1/userinfo?alt=json";

		var client = _httpClientFactory.CreateClient(HttpClients.External);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", notification.AccessToken);

		var response = await client.GetAsync(googleUrl, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			_logger.LogError("Failed to retrieve the Google profile picture for user with id {userId}. " +
							 "{statusCode}: {reasonPhrase}", notification.ProviderKey, response.StatusCode, response.ReasonPhrase);
			return null;
		}

		var profilePictureResponse =
			await response.Content.ReadFromJsonAsync<GooglePictureResponse>(
				cancellationToken: cancellationToken);

		if (profilePictureResponse?.Picture is null)
		{
			_logger.LogWarning("Failed to retrieve the Google profile picture for user with id {userId}." +
							   "The response was successful, but was not in the expected format." +
							   "Received JSON: {response}", notification.ProviderKey,
				await response.Content.ReadAsStringAsync(cancellationToken));
			return null;
		}

		var imageStream = await GetImageStreamFromUrlAsync(
			profilePictureResponse.Picture,
			cancellationToken);

		if (imageStream is null)
		{
			return ProfileConstants.Default_Profile_Picture;
		}

		var resizedImage = await ResizeImageAsync(imageStream, cancellationToken);

		var imageUrl =
			await _imageService.UploadImageAsync(notification.UserId,
												 ProfileImageService.UserProfilePicturesContainerName,
												 resizedImage,
												 cancellationToken);

		await resizedImage.DisposeAsync();

		return imageUrl;
	}

	private async Task<string?> GetMicrosoftProfilePictureUrlAsync(AccessTokenAcquired notification,
		CancellationToken cancellationToken)
	{
		const string microsoftUrl = "https://graph.microsoft.com/v1.0/me/photo/$value";

		var client = _httpClientFactory.CreateClient(HttpClients.External);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", notification.AccessToken);

		var response = await client.GetAsync(microsoftUrl, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			_logger.LogError("Failed to retrieve the Microsoft profile picture for user with id {userId}. " +
							 "{statusCode}: {reasonPhrase}",
							 notification.ProviderKey,
							 response.StatusCode,
							 response.ReasonPhrase);

			return null;
		}

		await using var imageAsStream = await response.Content.ReadAsStreamAsync(cancellationToken);

		var resizedImage = await ResizeImageAsync(imageAsStream, cancellationToken);

		var imageUrl = await _imageService.UploadImageAsync(notification.UserId,
															ProfileImageService.UserProfilePicturesContainerName,
															resizedImage,
															cancellationToken);

		await resizedImage.DisposeAsync();

		return imageUrl;
	}

	private static async Task<Stream> ResizeImageAsync(Stream imageAsStream, CancellationToken cancellationToken)
	{
		// 0 maintains aspect ratio
		const int width = 0;
		const int height = 200;

		using var image = await Image.LoadAsync(imageAsStream, cancellationToken);

		var outputStream = new MemoryStream();

		image.Mutate(img => img.Resize(width, height));

		await image.SaveAsync(outputStream, new WebpEncoder(), cancellationToken);

		// Allow the stream to be read again
		outputStream.Position = 0;

		return outputStream;
	}

	private async Task<Stream?> GetImageStreamFromUrlAsync(string url, CancellationToken cancellationToken)
	{
		var client = _httpClientFactory.CreateClient(HttpClients.External);
		var response = await client.GetAsync(url, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			_logger.LogError("Failed to get image as byte array from url {url}", url);
			return null;
		}

		return await response.Content.ReadAsStreamAsync(cancellationToken);
	}
}
