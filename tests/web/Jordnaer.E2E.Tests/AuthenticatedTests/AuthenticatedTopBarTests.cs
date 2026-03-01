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
	private static async Task WithPageAsync(Func<IPage, Task> test)
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		try
		{
			await page.GotoAsync(SetUpFixture.BaseUrl);
			await test(page);
		}
		finally
		{
			await page.CloseAsync();
		}
	}

	[Test]
	[TestCase("Chat", ".*/chat")]
	public Task Links_Should_Be_Visible_In_The_Topbar_And_Redirect_Correctly(string linkName, string redirectUrlRegex) =>
		WithPageAsync(async page =>
		{
			// For links that appear in both desktop and mobile navigation, use First to pick the desktop version
			var link = page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = linkName }).First;

			await Expect(link).ToBeVisibleAsync();

			await link.ClickAsync();

			await Expect(page).ToHaveURLAsync(new Regex(redirectUrlRegex));
		});

	[Test]
	public Task Profile_Link_In_Dropdown_Redirects_To_Profile() =>
		WithPageAsync(async page =>
		{
			// Open the desktop profile dropdown via the button label (target .profile-menu-button to avoid the backdrop label)
			var profileMenuButton = page.Locator(".topbar-desktop .profile-menu-button");
			await profileMenuButton.ClickAsync();

			// Click the Profil link inside the dropdown
			var profileLink = page.Locator(".profile-menu-dropdown").GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Profil" }).First;
			await Expect(profileLink).ToBeVisibleAsync();
			await profileLink.ClickAsync();

			await Expect(page).ToHaveURLAsync(new Regex(".*/profile"));
		});

	[Test]
	public Task Logout_Link_Should_Be_In_Profile_Dropdown() =>
		WithPageAsync(async page =>
		{
			// Open the desktop profile dropdown via the button label (target .profile-menu-button to avoid the backdrop label)
			var profileMenuButton = page.Locator(".topbar-desktop .profile-menu-button");
			await profileMenuButton.ClickAsync();

			// Find and verify logout link is visible in dropdown
			var logoutLink = page.Locator(".profile-menu-dropdown").GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Log ud" }).First;
			await Expect(logoutLink).ToBeVisibleAsync();
		});
}
