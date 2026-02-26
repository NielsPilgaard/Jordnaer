using Microsoft.Playwright;

namespace Jordnaer.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Top Bar navigation component.
/// Encapsulates selectors and actions to reduce coupling between tests and UI structure.
/// </summary>
public class TopBarPage(IPage page)
{
	// Unauthenticated links
	private ILocator LoginLink => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Log ind" }).First;
	private ILocator RegisterLink => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Opret konto" }).First;

	// Notification bell
	private ILocator NotificationBell => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Notifikationer" });
	private ILocator NotificationBadge => page.Locator(".notification-bell-container .mud-badge-content");
	private ILocator NotificationDropdown => page.Locator(".notification-dropdown");
	private ILocator MarkAllAsReadButton => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Markér alle som læst" });

	// Profile dropdown
	private ILocator ProfileMenuLabel => page.Locator("label[for='profile-menu-toggle']");
	private ILocator ProfileMenuDropdown => page.Locator(".profile-menu-dropdown");

	public async Task NavigateAsync(string baseUrl)
	{
		await page.GotoAsync($"{baseUrl}/");
		await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
	}

	public async Task OpenNotificationsAsync()
	{
		await NotificationBell.ClickAsync();
		await NotificationDropdown.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
	}

	public async Task OpenProfileMenuAsync()
	{
		await ProfileMenuLabel.ClickAsync();
		await ProfileMenuDropdown.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
	}

	public async Task ClickMarkAllAsReadAsync()
	{
		await MarkAllAsReadButton.ClickAsync();
	}

	public ILocator GetLoginLink() => LoginLink;
	public ILocator GetRegisterLink() => RegisterLink;
	public ILocator GetNotificationBell() => NotificationBell;
	public ILocator GetNotificationBadge() => NotificationBadge;
	public ILocator GetNotificationDropdown() => NotificationDropdown;
	public ILocator GetMarkAllAsReadButton() => MarkAllAsReadButton;
	public ILocator GetProfileMenuDropdown() => ProfileMenuDropdown;
}
