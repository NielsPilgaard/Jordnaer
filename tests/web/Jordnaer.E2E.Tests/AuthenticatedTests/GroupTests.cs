using System.Text.RegularExpressions;
using Jordnaer.Database;
using Jordnaer.E2E.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests.AuthenticatedTests;

[Parallelizable(ParallelScope.None)]
[TestFixture]
[Category(nameof(TestCategory.UI))]
[Category(nameof(TestCategory.Authenticated))]
public class GroupTests : PlaywrightTest
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
	public async Task Created_Group_Appears_In_My_Groups()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		var groupPage = page.CreateGroupPage();

		await groupPage.NavigateToMyGroupsAsync(SetUpFixture.BaseUrl);

		await Expect(groupPage.GetGroupByName(TestGroupName)).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 15_000 });

		await page.CloseAsync();
	}

	[OneTimeTearDown]
	public async Task Cleanup()
	{
		await using var scope = SetUpFixture.Services.CreateAsyncScope();
		var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JordnaerDbContext>>();
		await using var context = await dbContextFactory.CreateDbContextAsync();

		var group = await context.Groups.FirstOrDefaultAsync(g => g.Name == TestGroupName);
		if (group is not null)
		{
			context.Groups.Remove(group);
			await context.SaveChangesAsync();
		}
	}
}
