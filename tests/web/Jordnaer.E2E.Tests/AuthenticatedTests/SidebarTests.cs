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
public class AuthenticatedSidebarTests : BrowserTest
{
	[Test]
	public async Task My_Groups_Link_Should_Be_Visible_In_The_Sidebar_And_Redirect_Correctly()
	{
		var page = await SetUpFixture.Browser.NewPageAsync(Playwright);
		await page.GotoAsync(TestConfiguration.Values.BaseUrl);

		await page.GetByRole(AriaRole.Banner).GetByRole(AriaRole.Button).ClickAsync();
		await Expect(page.GetByRole(AriaRole.Link, new PageGetByRoleOptions
		{
			Name = "Mine Grupper"
		})).ToBeVisibleAsync();

		await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions
		{
			Name = "Mine Grupper"
		}).ClickAsync();

		await Expect(page).ToHaveURLAsync(new Regex(".*/personal/groups"));
	}

	[Test]
	public async Task Chat_Link_Should_Be_Visible_In_The_Sidebar_And_Redirect_Correctly()
	{
		var page = await SetUpFixture.Browser.NewPageAsync(Playwright);
		await page.GotoAsync(TestConfiguration.Values.BaseUrl);

		await page.GetByRole(AriaRole.Banner).GetByRole(AriaRole.Button).ClickAsync();

		await Expect(page.GetByRole(AriaRole.Link, new PageGetByRoleOptions
		{
			Name = "Chat"
		})).ToBeVisibleAsync();

		await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions
		{
			Name = "Chat"
		}).ClickAsync();

		await Expect(page).ToHaveURLAsync(new Regex(".*/chat"));
	}
}