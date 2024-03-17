namespace Jordnaer.Features.Images;

public interface IExternalProviderPictureDownloader
{
	ValueTask<string?> GetProfilePictureUrlAsync(
		AccessTokenAcquired accessTokenAcquired,
		ExternalProvider externalProvider,
		CancellationToken cancellationToken = default);
}