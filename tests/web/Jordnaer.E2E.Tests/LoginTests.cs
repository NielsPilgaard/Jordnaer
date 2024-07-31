using System.Text.RegularExpressions;
using Jordnaer.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests;

[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category(nameof(TestCategory.UI))]
public class LoginTests : BrowserTest
{
	[Test]
	public async Task When_User_Logs_In_With_Email_And_Password_It_Succeeds()
		=> await Browser.Login(Playwright);

	[Test]
	[TestCase("Facebook")]
	[TestCase("Microsoft")]
	[TestCase("Google")]
	public async Task When_User_Goes_To_Login_External_Provider_Login_Is_Visible(string externalProvider)
	{
		var page = await SetUpFixture.Browser.NewPageAsync(Playwright, false);
		await page.GotoAsync(TestConfiguration.Values.BaseUrl);
		await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Opret ny konto" }).ClickAsync();

		await Expect(page.GetByRole(AriaRole.Button, new PageGetByRoleOptions
		{
			Name = $"Log ind med {externalProvider}"
		})).ToBeVisibleAsync();

		await page.CloseAsync();
	}

	[Test]
	[TestCase("Glemt")]
	[TestCase("Opret")]
	// Æ is not supported, so we use dot as a regex wildcard
	[TestCase("Gensend emailbekr.ftelse")]
	public async Task When_User_Goes_To_Login_Links_Are_Visible(string linkTextRegex)
	{
		var page = await SetUpFixture.Browser.NewPageAsync(Playwright, false);

		await page.GotoAsync(TestConfiguration.Values.BaseUrl);
		await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Opret ny konto" }).ClickAsync();
		await page.GetByText("Log ind med eksisterende konto").ClickAsync();

		await Expect(page.GetByRole(AriaRole.Link, new PageGetByRoleOptions
		{
			NameRegex = new Regex(linkTextRegex)
		})).ToBeVisibleAsync();

		await page.CloseAsync();
	}
}
