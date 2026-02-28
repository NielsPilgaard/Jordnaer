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
public class TopBarTests : PlaywrightTest
{
	[Test]
	[TestCase("Chat", ".*/chat")]
	public async Task Links_Should_Be_Visible_In_The_Topbar_And_Redirect_Correctly(string linkName, string redirectUrlRegex)
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		await page.GotoAsync(SetUpFixture.BaseUrl);

		// For links that appear in both desktop and mobile navigation, use First to pick the desktop version
		var link = page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = linkName }).First;

		await Expect(link).ToBeVisibleAsync();

		await link.ClickAsync();

		await Expect(page).ToHaveURLAsync(new Regex(redirectUrlRegex));

		await page.CloseAsync();
	}

	[Test]
	public async Task Profile_Link_In_Dropdown_Redirects_To_Profile()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		await page.GotoAsync(SetUpFixture.BaseUrl);

		// Open the desktop profile dropdown by clicking its button label (use Last to avoid the backdrop)
		var profileMenuButton = page.Locator("label[for='profile-menu-toggle']").Last;
		await profileMenuButton.ClickAsync();

		// Click the Profil link inside the dropdown
		var profileLink = page.Locator(".profile-menu-dropdown").GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Profil" }).First;
		await Expect(profileLink).ToBeVisibleAsync();
		await profileLink.ClickAsync();

		await Expect(page).ToHaveURLAsync(new Regex(".*/profile"));

		await page.CloseAsync();
	}

	[Test]
	public async Task Logout_Link_Should_Be_In_Profile_Dropdown()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		await page.GotoAsync(SetUpFixture.BaseUrl);

		// Open the desktop profile dropdown by clicking its button label (use Last to avoid the backdrop)
		var profileMenuButton = page.Locator("label[for='profile-menu-toggle']").Last;
		await profileMenuButton.ClickAsync();

		// Find and verify logout link is visible in dropdown
		var logoutLink = page.Locator(".profile-menu-dropdown").GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Log ud" }).First;
		await Expect(logoutLink).ToBeVisibleAsync();

		await page.CloseAsync();
	}
}
