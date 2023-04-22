using System.Diagnostics;
using System.Net.Http.Headers;
using Jordnaer.Server.Database;
using Jordnaer.Server.Extensions;
using Jordnaer.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Server.Features.Profile;

public readonly struct AccessTokenAcquired : INotification
{
    public readonly string ApplicationUserId;
    public readonly string ProviderUserId;
    public readonly string Provider;
    public readonly string AccessToken;

    public AccessTokenAcquired(string applicationUserId, string providerUserId, string provider, string accessToken)
    {
        Debug.Assert(!string.IsNullOrEmpty(applicationUserId));
        Debug.Assert(!string.IsNullOrEmpty(providerUserId));
        Debug.Assert(!string.IsNullOrEmpty(provider));
        Debug.Assert(!string.IsNullOrEmpty(accessToken));

        ApplicationUserId = applicationUserId;
        ProviderUserId = providerUserId;
        Provider = provider;
        AccessToken = accessToken;
    }
}

public class ExternalProfilePictureDownloader : INotificationHandler<AccessTokenAcquired>
{
    private readonly JordnaerDbContext _context;
    private readonly ILogger<ExternalProfilePictureDownloader> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public ExternalProfilePictureDownloader(
        JordnaerDbContext context,
        ILogger<ExternalProfilePictureDownloader> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async ValueTask Handle(AccessTokenAcquired notification, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SupportedAuthProviders>(notification.Provider, out var provider))
        {
            _logger.LogError("Failed to parse provider {provider} to SupportedAuthProviders enum. " +
                             "Valid values: {supportedAuthProviders}",
                notification.Provider, string.Join(", ", Enum.GetValues<SupportedAuthProviders>()));
            return;
        }

        var user = await _context.UserProfiles.FirstOrDefaultAsync(
            parent => parent.ApplicationUserId == notification.ApplicationUserId,
            cancellationToken);

        user ??= new UserProfile { ApplicationUserId = notification.ApplicationUserId };

        string? profilePictureUrl = provider switch
        {
            SupportedAuthProviders.Facebook => await GetFacebookProfilePictureUrlAsync(
                notification.ProviderUserId,
                notification.AccessToken,
                cancellationToken),

            SupportedAuthProviders.Google => await GetGoogleProfilePictureUrlAsync(
                notification.ProviderUserId,
                notification.AccessToken,
                cancellationToken),

            SupportedAuthProviders.Microsoft => await GetMicrosoftProfilePictureUrlAsync(
                notification.ProviderUserId,
                notification.AccessToken,
                cancellationToken),

            _ => throw new UnreachableException($"Encountered unexpected enum value {provider} in enum {nameof(SupportedAuthProviders)}")
        };

        if (profilePictureUrl is null)
        {
            _logger.LogWarning("Failed to retrieve the {provider} profile picture for user with id {userId}", provider.ToString(), user.ApplicationUserId);

            return;
        }

        user.ProfilePictureUrl = profilePictureUrl;
        _context.UserProfiles.Update(user);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<string?> GetMicrosoftProfilePictureUrlAsync(string userId, string accessToken,
        CancellationToken cancellationToken)
    {
        const string microsoftUrl = $"https://graph.microsoft.com/v1.0/me/photo/$value";

        var client = _httpClientFactory.CreateClient(HttpClients.EXTERNAL);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync(microsoftUrl, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to retrieve the Microsoft profile picture for user with id {userId}. " +
                             "Response: {@response}", userId, response);
            return null;
        }

        byte[] profilePictureResponse =
            await response.Content.ReadAsByteArrayAsync(cancellationToken);

        string base64Image = Convert.ToBase64String(profilePictureResponse);
        string imageDataUrl = $"data:image/jpeg;base64,{base64Image}";

        return imageDataUrl;
    }

    private async Task<string?> GetGoogleProfilePictureUrlAsync(string userId, string accessToken,
        CancellationToken cancellationToken)
    {
        const string googleUrl = "https://www.googleapis.com/oauth2/v1/userinfo?alt=json";

        var client = _httpClientFactory.CreateClient(HttpClients.EXTERNAL);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync(googleUrl, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to retrieve the Google profile picture for user with id {userId}. " +
                             "Response: {@response}", userId, response);
            return null;
        }

        var profilePictureResponse =
            await response.Content.ReadFromJsonAsync<GooglePictureResponse>(
                cancellationToken: cancellationToken);

        if (profilePictureResponse?.Picture is not null)
        {
            return profilePictureResponse.Picture;
        }

        _logger.LogWarning("Failed to retrieve the Google profile picture for user with id {userId}." +
                           "The response was successful, but was not in the expected format." +
                           "Received JSON: {response}", userId,
            await response.Content.ReadAsStringAsync(cancellationToken));

        return null;
    }

    public async Task<string?> GetFacebookProfilePictureUrlAsync(string userId, string accessToken,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClients.EXTERNAL);
        string facebookUrl = $"https://graph.facebook.com/v13.0/{userId}/picture?" +
                            $"type=normal&" +
                            $"redirect=false&" +
                            $"access_token={accessToken}";

        var response = await client.GetAsync(facebookUrl, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to retrieve the Facebook profile picture for user with id {userId}. " +
                             "Response: {@response}", userId, response);
            return null;
        }

        var profilePictureResponse =
            await response.Content.ReadFromJsonAsync<FacebookProfilePictureResponse>(
                cancellationToken: cancellationToken);

        if (profilePictureResponse?.Data?.Url is not null)
        {
            return profilePictureResponse.Data?.Url;
        }

        _logger.LogWarning("Failed to retrieve the Facebook profile picture for user with id {userId}." +
                           "The response was successful, but was not in the expected format." +
                           "Received JSON: {response}", userId,
            await response.Content.ReadAsStringAsync(cancellationToken));
        return null;
    }
}
