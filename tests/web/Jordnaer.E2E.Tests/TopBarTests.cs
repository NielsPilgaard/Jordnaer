using System.Text.RegularExpressions;
using Jordnaer.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests;

[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category(nameof(TestCategory.UI))]
public class AuthenticatedTopBarTests : BrowserTest
{
	[Test]
	[TestCase("Personer", ".*/users")]
	[TestCase("Grupper", ".*/groups")]
	[TestCase("Opret ny konto", ".*/Account/Register")]
	public async Task Links_Should_Be_Visible_In_The_Topbar_And_Redirect_Correctly(string linkName, string redirectUrlRegex)
	{
		var page = await SetUpFixture.Browser.NewPageAsync(null, false);

		await page.GotoAsync(TestConfiguration.Values.BaseUrl);

		var link = page.GetByRole(AriaRole.Link,
									  new PageGetByRoleOptions
									  {
										  Name = linkName
									  }).First;

		await Expect(link).ToBeVisibleAsync();

		await link.ClickAsync();

		await Expect(page).ToHaveURLAsync(new Regex(redirectUrlRegex));

		await page.CloseAsync();
	}
}