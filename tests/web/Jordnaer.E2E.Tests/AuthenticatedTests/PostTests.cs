using Jordnaer.E2E.Tests.Infrastructure;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests.AuthenticatedTests;

[Parallelizable(ParallelScope.None)]
[TestFixture]
[Category(nameof(TestCategory.UI))]
[Category(nameof(TestCategory.Authenticated))]
public class PostTests : BrowserTest
{
	private const string TestPostContent = "Dette er en automatisk E2E test opslag";

	[Test]
	[Order(1)]
	public async Task Create_Post_Appears_In_Feed()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		var postPage = page.CreatePostPage();

		await postPage.NavigateAsync(SetUpFixture.BaseUrl);
		await postPage.CreatePostAsync(TestPostContent);

		await Expect(postPage.GetPostWithContent(TestPostContent)).ToBeVisibleAsync();

		await page.CloseAsync();
	}

	[Test]
	[Order(2)]
	public async Task Delete_Post_Removes_It_From_Feed()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		var postPage = page.CreatePostPage();

		await postPage.NavigateAsync(SetUpFixture.BaseUrl);

		// Ensure the post exists first
		var postLocator = postPage.GetPostWithContent(TestPostContent);
		if (!await postLocator.IsVisibleAsync())
		{
			// Create it if not present from previous test
			await postPage.CreatePostAsync(TestPostContent);
		}

		await postPage.DeletePostAsync(TestPostContent);

		await Expect(postPage.GetPostWithContent(TestPostContent)).ToBeHiddenAsync();

		await page.CloseAsync();
	}
}
