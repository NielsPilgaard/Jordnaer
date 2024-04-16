using Jordnaer.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests;

[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category(nameof(TestCategory.UI))]
[Category(nameof(TestCategory.SkipInCi))]
public class GroupSearchTests : BrowserTest
{
	[Test]
	public async Task When_User_Searches_For_Random_Guid_Empty_Alert_Is_Returned()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		await page.GotoAsync($"{TestConfiguration.Values.BaseUrl}/groups");

		await Task.Delay(500);

		// Search for random name
		await page.GetByRole(AriaRole.Textbox).First.FillAsync(Guid.NewGuid().ToString());

		// Click search button
		await page.GetByRole(AriaRole.Group).GetByRole(AriaRole.Button).First.ClickAsync();

		// Expect empty result
		await Expect(page.GetByRole(AriaRole.Alert)).ToBeVisibleAsync();

		await page.CloseAsync();
	}
}
