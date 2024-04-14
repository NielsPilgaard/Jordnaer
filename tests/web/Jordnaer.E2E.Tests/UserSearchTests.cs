using Jordnaer.E2E.Tests.Infrastructure;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests;

[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category(nameof(TestCategory.UI))]
public class UserSearchTests : BrowserTest
{
	[Test]
	public async Task When_User_Clicks_Search_Results_Are_Returned()
	{
		var page = await Browser.NewPageAsync(Playwright);
		await page.GotoAsync(TestConfiguration.Values.BaseUrl);
	}
}
