using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Text.RegularExpressions;
using Jordnaer.E2E.Tests.Infrastructure;

namespace Jordnaer.E2E.Tests;

[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category(nameof(TestCategory.UI))]
public partial class LandingPageTests : BrowserTest
{
	[GeneratedRegex(".*/Account/Login")]
	private partial Regex LoginRegex();

	[GeneratedRegex(".*/posts")]
	private partial Regex PostsRegex();

	[GeneratedRegex(".*/groups")]
	private partial Regex GroupsRegex();

	[Test]
	public async Task When_User_Clicks_Join_User_Should_Be_Redirected_To_Login()
	{
		var page = await Browser.NewPageAsync(Playwright);

		await page.GotoAsync(TestConfiguration.Values.BaseUrl);

		await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "VÆR' MED" }).ClickAsync();

		await Expect(page).ToHaveURLAsync(LoginRegex());
	}

	[Test]
	[Ignore("The Posts navlink is currently hidden")]
	public async Task When_User_Clicks_Posts_User_Should_Be_Redirected_To_Posts()
	{
		var page = await Browser.NewPageAsync(Playwright);

		await page.GotoAsync(TestConfiguration.Values.BaseUrl);

		await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "OPSLAG" }).ClickAsync();

		await Expect(page).ToHaveURLAsync(PostsRegex());
	}

	[Test]
	public async Task When_User_Clicks_Groups_User_Should_Be_Redirected_To_Groups()
	{
		var page = await Browser.NewPageAsync(Playwright);

		await page.GotoAsync(TestConfiguration.Values.BaseUrl);

		await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions
		{
			Name = "GRUPPER", Exact = true
		}).ClickAsync();

		await Expect(page).ToHaveURLAsync(GroupsRegex());
	}

	[Test]
	public async Task Topbar_Logo_Should_Not_Be_Visible()
	{
		var page = await Browser.NewPageAsync(Playwright);

		await page.GotoAsync(TestConfiguration.Values.BaseUrl);

		await Expect(page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Logo" })).ToBeHiddenAsync();
	}

	[Test]
	public async Task Topbar_Menu_Should_Be_Clickable_And_Have_Links()
	{
		var page = await Browser.NewPageAsync(Playwright);

		await page.GotoAsync(TestConfiguration.Values.BaseUrl);

		await Expect(page.GetByRole(AriaRole.Banner).GetByRole(AriaRole.Button)).ToBeVisibleAsync();
		await Expect(page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Personer" }))
			.ToBeVisibleAsync();
		await Expect(page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Grupper", Exact = true }))
			.ToBeVisibleAsync();
	}

	[Test]
	[TestCase("Om")]
	[TestCase("Privatlivspolitik")]
	[TestCase("Servicevilkår")]
	[TestCase("Kontakt")]
	[TestCase("Sponsorer")]
	[TestCase("Drift")]
	public async Task When_User_Is_On_The_LandingPage_Footer_Links_Should_Be_Visible_At_Very_The_Bottom(
		string linkText)
	{
		var page = await Browser.NewPageAsync(Playwright);
		await page.GotoAsync(TestConfiguration.Values.BaseUrl);

		await Expect(page.GetByRole(AriaRole.Link, new PageGetByRoleOptions
		{
			Name = linkText
		})).ToBeVisibleAsync();
	}
}
