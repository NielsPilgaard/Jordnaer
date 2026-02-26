using Jordnaer.E2E.Tests.Infrastructure;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests.AuthenticatedTests;

[Parallelizable(ParallelScope.None)]
[TestFixture]
[Category(nameof(TestCategory.UI))]
[Category(nameof(TestCategory.Authenticated))]
public class ProfileTests : BrowserTest
{
	[Test]
	public async Task Profile_Page_Shows_Edit_Form()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		var profilePage = page.CreateProfilePage();

		await profilePage.NavigateAsync(SetUpFixture.BaseUrl);

		await Expect(profilePage.GetFirstNameField()).ToBeVisibleAsync();
		await Expect(profilePage.GetLastNameField()).ToBeVisibleAsync();
		await Expect(profilePage.GetSaveButton()).ToBeVisibleAsync();

		await page.CloseAsync();
	}

	[Test]
	public async Task Profile_Page_Can_Update_First_Name()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		var profilePage = page.CreateProfilePage();

		await profilePage.NavigateAsync(SetUpFixture.BaseUrl);

		const string updatedName = "UpdatedUser";
		await profilePage.FillFirstNameAsync(updatedName);
		await profilePage.SaveAsync();

		// Reload and verify the change persisted
		await profilePage.NavigateAsync(SetUpFixture.BaseUrl);

		await Expect(profilePage.GetFirstNameField()).ToHaveValueAsync(updatedName);

		// Restore original name
		await profilePage.FillFirstNameAsync("User");
		await profilePage.SaveAsync();

		await page.CloseAsync();
	}
}
