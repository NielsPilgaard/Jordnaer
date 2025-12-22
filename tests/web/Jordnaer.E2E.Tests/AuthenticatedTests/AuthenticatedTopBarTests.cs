using System.Text.RegularExpressions;
using Jordnaer.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests.AuthenticatedTests;

[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category(nameof(TestCategory.UI))]
[Category(nameof(TestCategory.Authenticated))]
[Category(nameof(TestCategory.SkipInCi))]
public class TopBarTests : BrowserTest
{
	[Test]
	[TestCase("Chat", ".*/chat")]
	[TestCase("Profil", ".*/profile")]
	public async Task Links_Should_Be_Visible_In_The_Topbar_And_Redirect_Correctly(string linkName, string redirectUrlRegex)
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		await page.GotoAsync(TestConfiguration.Values.BaseUrl);

		// For links that appear in both desktop and mobile navigation, use First to pick the desktop version
		var link = page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = linkName }).First;

		await Expect(link).ToBeVisibleAsync();

		await link.ClickAsync();

		await Expect(page).ToHaveURLAsync(new Regex(redirectUrlRegex));

		await page.CloseAsync();
	}

	[Test]
	public async Task Logout_Link_Should_Be_In_Profile_Dropdown()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		await page.GotoAsync(TestConfiguration.Values.BaseUrl);

		// Open the profile dropdown menu by clicking the label (the label intercepts clicks on the checkbox)
		var profileMenuLabel = page.Locator("label[for='profile-menu-toggle']");
		await profileMenuLabel.ClickAsync();

		// Find and verify logout link is visible in dropdown
		var logoutLink = page.Locator(".profile-menu-dropdown").GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Log ud" });
		await Expect(logoutLink).ToBeVisibleAsync();

		await page.CloseAsync();
	}
}
