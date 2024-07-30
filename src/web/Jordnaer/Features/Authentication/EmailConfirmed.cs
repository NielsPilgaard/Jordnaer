namespace Jordnaer.Features.Authentication;

public class ConfirmEmailStateContainer
{
	public string? UserId { get; private set; }

	public event Action? OnEmailConfirmed;

	public void EmailConfirmed(string userId)
	{
		UserId = userId;
		NotifyStateChanged();
	}

	private void NotifyStateChanged() => OnEmailConfirmed?.Invoke();
}