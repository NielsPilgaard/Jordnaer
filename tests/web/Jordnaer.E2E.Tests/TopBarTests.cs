using Jordnaer.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests;

[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category(nameof(TestCategory.UI))]
public class TopBarTests : BrowserTest
{
	[Test]
	public async Task Unauthenticated_Topbar_Shows_Register_Button()
	{
		var page = await SetUpFixture.Browser.NewPageAsync(Playwright, loadAuthenticationState: false);
		var topBar = page.CreateTopBarPage();

		await topBar.NavigateAsync(SetUpFixture.BaseUrl);

		await Expect(topBar.GetRegisterLink()).ToBeVisibleAsync();

		await page.CloseAsync();
	}

	[Test]
	public async Task Unauthenticated_Topbar_Shows_Register_Link()
	{
		var page = await SetUpFixture.Browser.NewPageAsync(Playwright, loadAuthenticationState: false);
		var topBar = page.CreateTopBarPage();

		await topBar.NavigateAsync(SetUpFixture.BaseUrl);

		await Expect(topBar.GetRegisterLink()).ToBeVisibleAsync();

		await page.CloseAsync();
	}

	[Test]
	public async Task Authenticated_Topbar_Shows_Notification_Bell()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		var topBar = page.CreateTopBarPage();

		await topBar.NavigateAsync(SetUpFixture.BaseUrl);

		await Expect(topBar.GetNotificationBell()).ToBeVisibleAsync();

		await page.CloseAsync();
	}
}
