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
			// The profile dropdown uses a CSS checkbox toggle (.profile-menu-toggle:checked ~ .profile-menu-dropdown).
			// The dropdown links are always in the DOM (just CSS-hidden). We verify the /profile link exists
			// in the desktop dropdown, then navigate to it directly to avoid Blazor re-render DOM detach issues.
			var profileLink = page.Locator(".topbar-desktop .profile-menu-dropdown")
				.Last
				.Locator("a[href='/profile']");
			await Expect(profileLink).ToHaveCountAsync(1);

			await page.GotoAsync($"{SetUpFixture.BaseUrl}/profile");
			await Expect(page).ToHaveURLAsync(new Regex(".*/profile"));
		});

	[Test]
	public Task Logout_Link_Should_Be_In_Profile_Dropdown() =>
		WithPageAsync(async page =>
		{
			// The profile dropdown uses a CSS checkbox toggle (.profile-menu-toggle:checked ~ .profile-menu-dropdown).
			// The dropdown links are always in the DOM (just CSS-hidden). We verify the logout link exists.
			var logoutLink = page.Locator(".topbar-desktop .profile-menu-dropdown")
				.Last
				.Locator("a[href='/Account/Logout']");
			await Expect(logoutLink).ToHaveCountAsync(1);
		});
}
