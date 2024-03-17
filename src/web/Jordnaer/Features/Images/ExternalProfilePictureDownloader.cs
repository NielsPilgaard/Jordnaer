using Jordnaer.Database;
using Mediator;

namespace Jordnaer.Features.Images;

public class ExternalProfilePictureDownloader(
	JordnaerDbContext context,
	ILogger<ExternalProfilePictureDownloader> logger,
	IEnumerable<IExternalProviderPictureDownloader> providerPictureDownloader)
	: INotificationHandler<AccessTokenAcquired>
{
	public async ValueTask Handle(AccessTokenAcquired notification, CancellationToken cancellationToken)
	{
		if (!Enum.TryParse<ExternalProvider>(notification.LoginProvider, out var provider))
		{
			logger.LogError("Failed to parse provider {provider} to ExternalProvider enum. " +
							 "Valid values: {supportedAuthProviders}",
				notification.LoginProvider, string.Join(", ", Enum.GetValues<ExternalProvider>()));
			return;
		}

		var user = await context.UserProfiles.FindAsync([notification.UserId], cancellationToken);
		if (user is null)
		{
			logger.LogWarning("User has not been created yet, we cannot set their profile picture. " +
							 "UserId: {UserId} LoginProvider: {Provider}", notification.UserId, notification.LoginProvider);
			return;
		}

		string? profilePictureUrl = null;
		foreach (var externalProviderPictureDownloader in providerPictureDownloader)
		{
			profilePictureUrl = await externalProviderPictureDownloader
				.GetProfilePictureUrlAsync(notification,
										   provider,
										   cancellationToken);

			if (profilePictureUrl is not null)
			{
				break;
			}
		}

		if (profilePictureUrl is null)
		{
			logger.LogWarning("Failed to retrieve the {ExternalProvider} " +
							   "profile picture for user with id {UserId}",
							   provider.ToStringFast(), user.Id);
			return;
		}

		user.ProfilePictureUrl = profilePictureUrl;

		context.UserProfiles.Update(user);

		await context.SaveChangesAsync(cancellationToken);
	}
}