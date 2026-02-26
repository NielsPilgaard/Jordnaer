using Jordnaer.E2E.Tests.Infrastructure;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests.AuthenticatedTests;

[Parallelizable(ParallelScope.None)]
[TestFixture]
[Category(nameof(TestCategory.UI))]
[Category(nameof(TestCategory.Authenticated))]
public class NotificationTests : BrowserTest
{
	[Test]
	public async Task Notification_Bell_Badge_Appears_After_Receiving_Chat_Message()
	{
		// User A sends a chat message to User B
		var pageA = await SetUpFixture.Context.NewPageAsync();
		var chatPage = pageA.CreateChatPage();

		await chatPage.NavigateAsync(SetUpFixture.BaseUrl);
		await chatPage.SearchForUserAsync("User B");
		await chatPage.SelectUserFromSearchResultsAsync("User B");
		await chatPage.SendMessageAsync("Hej fra User A!");

		await pageA.CloseAsync();

		// User B should see a notification badge
		var pageB = await SetUpFixture.ContextB.NewPageAsync();
		var topBar = pageB.CreateTopBarPage();
		await topBar.NavigateAsync(SetUpFixture.BaseUrl);

		await Expect(topBar.GetNotificationBadge()).ToBeVisibleAsync();

		await pageB.CloseAsync();
	}

	[Test]
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
		// User A has no notifications by default (only User B receives them from A's messages)
		var page = await SetUpFixture.Context.NewPageAsync();
		var notificationsPage = page.CreateNotificationsPage();

		await notificationsPage.NavigateAsync(SetUpFixture.BaseUrl);

		// Mark all as read first to ensure clean state
		var markAllButton = notificationsPage.GetMarkAllAsReadButton();
		if (await markAllButton.IsVisibleAsync())
		{
			await notificationsPage.MarkAllAsReadAsync();
		}

		await Expect(page.GetByText("endigt")).ToBeVisibleAsync();

		await page.CloseAsync();
	}
}
