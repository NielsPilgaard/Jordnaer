using System.Text.RegularExpressions;
using Jordnaer.E2E.Tests.Infrastructure;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests.AuthenticatedTests;

[Parallelizable(ParallelScope.None)]
[TestFixture]
[Category(nameof(TestCategory.UI))]
[Category(nameof(TestCategory.Authenticated))]
public class GroupTests : BrowserTest
{
	private static readonly string TestGroupName = $"TestGruppe-{Guid.NewGuid().ToString("N")[..8]}";
	private const string TestGroupDescription = "En gruppe til E2E tests";

	[Test]
	[Order(1)]
	public async Task Create_Group_Appears_In_Group_List()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		var groupPage = page.CreateGroupPage();

		await groupPage.NavigateToCreateGroupAsync(SetUpFixture.BaseUrl);
		await groupPage.FillGroupNameAsync(TestGroupName);
		await groupPage.FillShortDescriptionAsync(TestGroupDescription);
		await groupPage.SubmitCreateGroupAsync();

		// After creation, we should be redirected to the group detail or groups list
		await Expect(page).ToHaveURLAsync(new Regex(".*/groups.*"));

		await page.CloseAsync();
	}

	[Test]
	[Order(2)]
	public async Task Search_Group_Returns_Created_Group()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		var groupPage = page.CreateGroupPage();

		await groupPage.NavigateToGroupsAsync(SetUpFixture.BaseUrl);
		await groupPage.SearchForGroupAsync(TestGroupName);

		await Expect(page.GetByText(TestGroupName).First).ToBeVisibleAsync();

		await page.CloseAsync();
	}
}
