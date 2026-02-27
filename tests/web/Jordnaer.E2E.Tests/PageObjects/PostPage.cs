using Microsoft.Playwright;

namespace Jordnaer.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Posts feed (/posts).
/// Encapsulates selectors and actions to reduce coupling between tests and UI structure.
/// </summary>
public class PostPage(IPage page)
{
	private const string PageUrl = "/posts";

	// Create post
	private ILocator CreatePostTrigger => page.GetByPlaceholder("Hvad tænker du på?");
	private ILocator PostTextEditor => page.Locator(".tiptap.ProseMirror").First;
	private ILocator SubmitPostButton => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Slå op" });

	// Post feed
	private ILocator PostCards => page.Locator(".warm-shadow.warm-rounded");

	public async Task NavigateAsync(string baseUrl)
	{
		await page.GotoAsync($"{baseUrl}{PageUrl}");
		await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
	}

	public async Task ExpandCreateFormAsync()
	{
		await CreatePostTrigger.ClickAsync();
		await PostTextEditor.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
	}

	public async Task FillPostContentAsync(string content)
	{
		await PostTextEditor.ClickAsync();
		await PostTextEditor.FillAsync(content);
	}

	public async Task SubmitPostAsync()
	{
		await SubmitPostButton.ClickAsync();
		await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
	}

	public async Task CreatePostAsync(string content)
	{
		await ExpandCreateFormAsync();
		await FillPostContentAsync(content);
		await SubmitPostAsync();
	}

	public async Task DeletePostAsync(string postContent)
	{
		// Find the post card containing the content
		var postCard = PostCards.Filter(new LocatorFilterOptions { HasText = postContent }).First;

		// Click the "Flere muligheder" button (three dots)
		var moreOptionsButton = postCard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Flere muligheder" });
		await moreOptionsButton.ClickAsync();

		// Click "Slet"
		await page.GetByRole(AriaRole.Menuitem, new PageGetByRoleOptions { Name = "Slet" }).ClickAsync();

		// Confirm deletion in dialog
		await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Slet" }).Last.ClickAsync();
		await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
	}

	public ILocator GetPostWithContent(string content) =>
		page.GetByText(content).First;

	public ILocator GetPostCards() => PostCards;
}
