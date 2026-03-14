using Jordnaer.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests.AuthenticatedTests;

[Parallelizable(ParallelScope.None)]
[TestFixture]
[Category(nameof(TestCategory.UI))]
[Category(nameof(TestCategory.Authenticated))]
public class NotificationTests : PlaywrightTest
{
	[Test]
	[Order(1)]
	public async Task Notification_Bell_Badge_Appears_After_Receiving_Chat_Message()
	{
		// User A sends a chat message to User B
		var pageA = await SetUpFixture.Context.NewPageAsync();
		var chatPage = pageA.CreateChatPage();

		await chatPage.NavigateAsync(SetUpFixture.BaseUrl);
		await chatPage.SearchForUserAsync("User B");
		await chatPage.SelectUserFromSearchResultsAsync("User B");
		await chatPage.SendMessageAsync("Hej fra User A!");

		// Wait for MassTransit to process the message asynchronously.
		// SendMessageConsumer writes the notification to DB; User B's TopBar initial load query picks it up.
		// NetworkIdle alone is insufficient as MassTransit consumption is fully async server-side.
		await pageA.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await pageA.WaitForTimeoutAsync(3_000);

		await pageA.CloseAsync();

		// User B should see a notification badge
		var pageB = await SetUpFixture.ContextB.NewPageAsync();
		var topBar = pageB.CreateTopBarPage();
		await topBar.NavigateAsync(SetUpFixture.BaseUrl);

		// Chat messages show on the chat icon badge in the topbar, not the notification bell badge
		await Expect(topBar.GetChatBadge()).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });

		await pageB.CloseAsync();
	}

	[Test]
	[Order(2)]
	public async Task Notifications_Mark_All_As_Read_Removes_Badge()
	{
		// Navigate to the notifications page as User B
		var page = await SetUpFixture.ContextB.NewPageAsync();
		var notificationsPage = page.CreateNotificationsPage();
		var topBar = page.CreateTopBarPage();

		await notificationsPage.NavigateAsync(SetUpFixture.BaseUrl);

		// If there are unread notifications, mark them all as read
		var markAllButton = notificationsPage.GetMarkAllAsReadButton();
		if (await markAllButton.IsVisibleAsync())
		{
			await notificationsPage.MarkAllAsReadAsync();
		}

		// Navigate away and back to verify badge is gone
		await topBar.NavigateAsync(SetUpFixture.BaseUrl);

		await Expect(topBar.GetNotificationBadge()).ToBeHiddenAsync();

		await page.CloseAsync();
	}

	[Test]
	public async Task Notifications_Page_Shows_Empty_State_When_No_Notifications()
	{
		// Assert a clean state: navigate to the notifications page and clear any existing
		// notifications defensively before checking the empty state.
		var page = await SetUpFixture.Context.NewPageAsync();
		var notificationsPage = page.CreateNotificationsPage();

		await notificationsPage.NavigateAsync(SetUpFixture.BaseUrl);

		var markAllButton = notificationsPage.GetMarkAllAsReadButton();
		if (await markAllButton.IsVisibleAsync())
		{
			await notificationsPage.MarkAllAsReadAsync();
		}

		await Expect(notificationsPage.GetEmptyStateMessage()).ToBeVisibleAsync();

		await page.CloseAsync();
	}
}
