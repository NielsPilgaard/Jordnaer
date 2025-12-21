using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Text.RegularExpressions;
using Jordnaer.E2E.Tests.Infrastructure;

namespace Jordnaer.E2E.Tests;

[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category(nameof(TestCategory.UI))]
public class LandingPageTests : BrowserTest
{
	[Test]
	public async Task When_User_Clicks_Join_User_Should_Be_Redirected_To_Login()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		var landingPage = page.CreateLandingPage();

		await landingPage.NavigateAsync(TestConfiguration.Values.BaseUrl);
		await landingPage.ClickJoinAsync();

		await Expect(page).ToHaveURLAsync(new Regex(".*/Account/Register"));

		await page.CloseAsync();
	}

	[Test]
	[Ignore("The Posts navlink is currently hidden")]
	public async Task When_User_Clicks_Posts_User_Should_Be_Redirected_To_Posts()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		var landingPage = page.CreateLandingPage();

		await landingPage.NavigateAsync(TestConfiguration.Values.BaseUrl);
		await landingPage.ClickPostsAsync();

		await Expect(page).ToHaveURLAsync(new Regex(".*/posts"));

		await page.CloseAsync();
	}

	[Test]
	public async Task When_User_Clicks_Groups_User_Should_Be_Redirected_To_Groups()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		var landingPage = page.CreateLandingPage();

		await landingPage.NavigateAsync(TestConfiguration.Values.BaseUrl);
		await landingPage.ClickGroupsAsync();

		await Expect(page).ToHaveURLAsync(new Regex(".*/groups"));

		await page.CloseAsync();
	}

	[Test]
	public async Task Topbar_Logo_Should_Not_Be_Visible()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		var landingPage = page.CreateLandingPage();

		await landingPage.NavigateAsync(TestConfiguration.Values.BaseUrl);

		await Expect(landingPage.GetLogoLink()).ToBeHiddenAsync();

		await page.CloseAsync();
	}

	[Test]
	public async Task Topbar_Menu_Should_Have_Links()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		var landingPage = page.CreateLandingPage();

		await landingPage.NavigateAsync(TestConfiguration.Values.BaseUrl);

		await Expect(landingPage.GetPeopleLink()).ToBeVisibleAsync();
		await Expect(landingPage.GetGroupsLink()).ToBeVisibleAsync();

		await page.CloseAsync();
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
		var page = await SetUpFixture.Context.NewPageAsync();
		var landingPage = page.CreateLandingPage();

		await landingPage.NavigateAsync(TestConfiguration.Values.BaseUrl);

		await Expect(landingPage.GetFooterLink(linkText)).ToBeVisibleAsync();

		await page.CloseAsync();
	}
}
