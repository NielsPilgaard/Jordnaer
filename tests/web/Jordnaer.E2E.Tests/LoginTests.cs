using System.Text.RegularExpressions;
using Jordnaer.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category(nameof(TestCategory.UI))]
public class LoginTests : PlaywrightTest
{
	[Test]
	public async Task When_User_Logs_In_With_Email_And_Password_It_Succeeds()
	{
		var authFilePath = Path.Combine(Path.GetTempPath(), $"auth-verify-{Guid.NewGuid():N}.json");
		try
		{
			await SetUpFixture.Browser.Login(
				SetUpFixture.Playwright,
				SetUpFixture.BaseUrl,
				E2eWebApplicationFactory.UserAEmail,
				E2eWebApplicationFactory.UserAPassword,
				authFilePath);

			Assert.That(File.Exists(authFilePath), Is.True, "Auth state file should have been created after login.");
			var authContent = await File.ReadAllTextAsync(authFilePath);
			Assert.That(authContent, Is.Not.Empty, "Auth state file should not be empty.");
			Assert.That(authContent, Does.Contain("cookies"), "Auth state file should contain cookie data.");
		}
		finally
		{
			if (File.Exists(authFilePath))
			{
				File.Delete(authFilePath);
			}
		}
	}

	[Test]
	[Category(nameof(TestCategory.SkipInCi))]
	[Category(nameof(TestCategory.ExternalLogin))]
	[TestCase("Facebook")]
	[TestCase("Microsoft")]
	[TestCase("Google")]
	public async Task When_User_Goes_To_Login_External_Provider_Login_Is_Visible(string externalProvider)
	{
		var page = await SetUpFixture.Browser.NewPageAsync(SetUpFixture.Playwright, false);
		try
		{
			await page.GotoAsync(SetUpFixture.BaseUrl);
			await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Opret konto" }).ClickAsync();

			await Expect(page.GetByRole(AriaRole.Button, new PageGetByRoleOptions
			{
				Name = $"Log ind med {externalProvider}"
			})).ToBeVisibleAsync();
		}
		finally
		{
			await page.CloseAsync();
		}
	}

	[Test]
	[TestCase("Glemt")]
	[TestCase("Opret")]
	// æ is not supported, so we use dot as a regex wildcard
	[TestCase("Gensend emailbekr.ftelse")]
	public async Task When_User_Goes_To_Login_Links_Are_Visible(string linkTextRegex)
	{
		var page = await SetUpFixture.Browser.NewPageAsync(SetUpFixture.Playwright, false);
		try
		{
			await page.GotoAsync(SetUpFixture.BaseUrl);
			await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Opret konto" }).ClickAsync();
			await page.GetByText("Log ind med eksisterende konto").ClickAsync();

			await Expect(page.GetByRole(AriaRole.Link, new PageGetByRoleOptions
			{
				NameRegex = new Regex(linkTextRegex)
			}).First).ToBeVisibleAsync();
		}
		finally
		{
			await page.CloseAsync();
		}
	}
}
