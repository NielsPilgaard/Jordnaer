using System.Diagnostics;
using Mediator;

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