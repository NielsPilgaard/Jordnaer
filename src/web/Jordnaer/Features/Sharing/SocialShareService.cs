using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using MudBlazor;

namespace Jordnaer.Features.Sharing;

public class SocialShareService(
    IJSRuntime jsRuntime,
    ISnackbar snackbar,
    IConfiguration configuration)
{
    private bool? _canUseNativeShare;
    private string? _facebookAppId;

    /// <summary>
    /// Initializes the service by checking for native share support and loading Facebook App ID.
    /// Should be called once during component initialization.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_canUseNativeShare.HasValue)
        {
            return; // Already initialized
        }

        _canUseNativeShare = await jsRuntime.InvokeAsync<bool>("utilities.canShare");
        _facebookAppId = configuration["Authentication:Schemes:Facebook:AppId"];
    }

    /// <summary>
    /// Shares content to Facebook using the Facebook Share Dialog.
    /// </summary>
    /// <param name="url">The URL to share</param>
    /// <param name="hashtag">Optional hashtag to include (e.g., "#MiniMøder")</param>
    public async Task ShareToFacebookAsync(string url, string? hashtag = null)
    {
        await InitializeAsync();

        string facebookUrl;
        if (!string.IsNullOrEmpty(_facebookAppId))
        {
            // Facebook Share Dialog - better preview and hashtag support
            var shareUrlBuilder = new StringBuilder();
            shareUrlBuilder.Append("https://www.facebook.com/dialog/share");
            shareUrlBuilder.Append($"?app_id={_facebookAppId}");
            shareUrlBuilder.Append("&display=popup");
            shareUrlBuilder.Append($"&href={Uri.EscapeDataString(url)}");
            // redirect_uri is required - redirect back to the shared page after sharing
            shareUrlBuilder.Append($"&redirect_uri={Uri.EscapeDataString(url)}");

            if (!string.IsNullOrEmpty(hashtag))
            {
                shareUrlBuilder.Append($"&hashtag={Uri.EscapeDataString(hashtag)}");
            }

            facebookUrl = shareUrlBuilder.ToString();
        }
        else
        {
            // Legacy sharer - fallback if no App ID
            var encodedUrl = Uri.EscapeDataString(url);
            facebookUrl = $"https://www.facebook.com/sharer/sharer.php?u={encodedUrl}";
        }

        await jsRuntime.InvokeVoidAsync("utilities.openShareWindow", facebookUrl, "facebook-share");
    }

    /// <summary>
    /// Shares content to Instagram. Uses native share on mobile, copies link on desktop.
    /// </summary>
    /// <param name="url">The URL to share</param>
    /// <param name="title">The title for the share (used in native share)</param>
    /// <param name="text">The text/description for the share (used in native share)</param>
    public async Task ShareToInstagramAsync(string url, string? title = null, string? text = null)
    {
        await InitializeAsync();

        if (_canUseNativeShare == true)
        {
            // On mobile: Use native share sheet which includes Instagram
            var shared = await jsRuntime.InvokeAsync<bool>(
                "utilities.nativeShare",
                title ?? "Mini Møder",
                text ?? "Se dette på Mini Møder",
                url);

            if (!shared)
            {
                // User cancelled or error - fall back to copy
                await CopyForInstagramAsync(url);
            }
        }
        else
        {
            // On desktop: Copy link with Instagram-specific message
            await CopyForInstagramAsync(url);
        }
    }

    /// <summary>
    /// Copies a URL to clipboard with an Instagram-specific message.
    /// </summary>
    private async Task CopyForInstagramAsync(string url)
    {
        var success = await jsRuntime.InvokeAsync<bool>("utilities.copyToClipboard", url);
        if (success)
        {
            snackbar.Add("Link kopieret! Du kan nu indsætte det på Instagram.", Severity.Success);
        }
        else
        {
            snackbar.Add("Kunne ikke kopiere linket. Prøv igen.", Severity.Error);
        }
    }

    /// <summary>
    /// Copies a URL to clipboard.
    /// </summary>
    /// <param name="url">The URL to copy</param>
    public async Task CopyLinkAsync(string url)
    {
        var success = await jsRuntime.InvokeAsync<bool>("utilities.copyToClipboard", url);
        if (success)
        {
            snackbar.Add("Link kopieret til udklipsholder!", Severity.Success);
        }
        else
        {
            snackbar.Add("Kunne ikke kopiere linket. Prøv igen.", Severity.Error);
        }
    }

    /// <summary>
    /// Gets whether the device supports native sharing.
    /// </summary>
    public bool CanUseNativeShare => _canUseNativeShare ?? false;
}
