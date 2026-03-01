using Jordnaer.Database;
using Jordnaer.E2E.Tests.Infrastructure;
using Jordnaer.Shared;
using MassTransit;
using Microsoft.AspNetCore.Identity;
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

	[OneTimeSetUp]
	public async Task SeedTestGroup()
	{
		await using var scope = SetUpFixture.Services.CreateAsyncScope();
		var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JordnaerDbContext>>();
		await using var context = await dbContextFactory.CreateDbContextAsync();

		// Get User A's ID so we can make them the group owner
		var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
		var userA = await userManager.FindByEmailAsync(E2eWebApplicationFactory.UserAEmail);
		if (userA is null)
		{
			Assert.Fail("User A not found in database");
			return;
		}

		// Skip if group already exists (re-run scenario)
		if (await context.Groups.AnyAsync(g => g.Name == TestGroupName))
		{
			return;
		}

		var groupId = NewId.NextGuid();
		var group = new Group
		{
			Id = groupId,
			Name = TestGroupName,
			ShortDescription = TestGroupDescription,
			ZipCode = 8000, // Aarhus C
			City = "Aarhus C",
			Memberships =
			[
				new GroupMembership
				{
					GroupId = groupId,
					UserProfileId = userA.Id,
					MembershipStatus = MembershipStatus.Active,
					OwnershipLevel = OwnershipLevel.Owner,
					PermissionLevel = PermissionLevel.Admin,
					UserInitiatedMembership = false
				}
			]
		};

		context.Groups.Add(group);
		await context.SaveChangesAsync();
	}

	[Test]
	[Order(1)]
	public async Task Create_Group_Appears_In_Group_List()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		var groupPage = page.CreateGroupPage();

		await groupPage.NavigateToMyGroupsAsync(SetUpFixture.BaseUrl);

		await Expect(groupPage.GetGroupByName(TestGroupName)).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 15_000 });

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
