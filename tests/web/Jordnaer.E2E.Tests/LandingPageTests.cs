using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Text.RegularExpressions;

namespace Jordnaer.E2E.Tests;

[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category(Constants.TestCategory)]
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
		var page = await Browser.NewPageAsync();

		await page.GotoAsync(Constants.MainUrl);

		await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "VÆR' MED" }).ClickAsync();

		await Expect(page).ToHaveURLAsync(LoginRegex());
	}

	[Test]
	[Ignore("The Posts navlink is currently hidden")]
	public async Task When_User_Clicks_Posts_User_Should_Be_Redirected_To_Posts()
	{
		var page = await Browser.NewPageAsync();

		await page.GotoAsync(Constants.MainUrl);

		await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "OPSLAG" }).ClickAsync();

		await Expect(page).ToHaveURLAsync(PostsRegex());
	}

	[Test]
	public async Task When_User_Clicks_Groups_User_Should_Be_Redirected_To_Groups()
	{
		var page = await Browser.NewPageAsync();

		await page.GotoAsync(Constants.MainUrl);

		await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions
		{
			Name = "GRUPPER", Exact = true
		}).ClickAsync();

		await Expect(page).ToHaveURLAsync(GroupsRegex());
	}
}
