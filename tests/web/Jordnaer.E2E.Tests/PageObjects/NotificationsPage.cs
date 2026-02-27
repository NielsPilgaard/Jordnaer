using Microsoft.Playwright;

namespace Jordnaer.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Notifications page (/notifications).
/// Encapsulates selectors and actions to reduce coupling between tests and UI structure.
/// </summary>
public class NotificationsPage(IPage page)
{
	private const string PageUrl = "/notifications";

	private ILocator MarkAllAsReadButton => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Markér alle som læst" });
	private ILocator UnreadItems => page.Locator(".notification-item.unread");
	private ILocator AllItems => page.Locator(".notification-item");
	private ILocator EmptyStateMessage => page.GetByText("Du har ingen notifikationer endnu");
	private ILocator NotificationBadge => page.Locator(".notification-bell-container .mud-badge-content");
	private ILocator NotificationBell => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Notifikationer" });
	private ILocator NotificationDropdown => page.Locator(".notification-dropdown");
	private ILocator MarkAllReadInDropdown => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Markér alle som læst" });

	public async Task NavigateAsync(string baseUrl)
	{
		await page.GotoAsync($"{baseUrl}{PageUrl}");
		await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
	}

	public async Task MarkAllAsReadAsync()
	{
		await MarkAllAsReadButton.ClickAsync();
		await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
	}

	public async Task OpenNotificationDropdownAsync()
	{
		await NotificationBell.ClickAsync();
		await NotificationDropdown.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
	}

	public async Task MarkAllAsReadViaDropdownAsync()
	{
		await OpenNotificationDropdownAsync();
		await MarkAllReadInDropdown.ClickAsync();
		await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
	}

	public ILocator GetMarkAllAsReadButton() => MarkAllAsReadButton;
	public ILocator GetUnreadItems() => UnreadItems;
	public ILocator GetAllItems() => AllItems;
	public ILocator GetEmptyStateMessage() => EmptyStateMessage;
	public ILocator GetNotificationBadge() => NotificationBadge;
	public ILocator GetNotificationDropdown() => NotificationDropdown;
}
