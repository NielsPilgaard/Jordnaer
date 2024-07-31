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
	[TestCase("Redigér Profil", ".*/profile")]
	[TestCase("Log ud", ".*")]
	public async Task Links_Should_Be_Visible_In_The_Topbar_And_Redirect_Correctly(string linkName, string redirectUrlRegex)
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		await page.GotoAsync(TestConfiguration.Values.BaseUrl);

		var link = page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = linkName });

		await Expect(link).ToBeVisibleAsync();

		await link.ClickAsync();

		await Expect(page).ToHaveURLAsync(new Regex(redirectUrlRegex));

		await page.CloseAsync();
	}
}