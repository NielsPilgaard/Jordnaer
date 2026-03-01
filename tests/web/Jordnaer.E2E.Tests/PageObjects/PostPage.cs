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
	private ILocator PostTextEditor => page.GetByPlaceholder("Del dine tanker...");
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
		var postCard = GetPostWithContent(postContent);
		await postCard.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

		// Click the "Flere muligheder" button (three dots) - MudMenu renders aria-label on wrapper div, button is inside it
		var moreOptionsButton = postCard.Locator("[aria-label='Flere muligheder'] button");
		await moreOptionsButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
		await moreOptionsButton.ClickAsync();

		// Wait for the MudMenu popover/list to appear, then click "Slet"
		var sletMenuItem = page.GetByRole(AriaRole.Menuitem, new PageGetByRoleOptions { Name = "Slet" });
		await sletMenuItem.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
		await sletMenuItem.ClickAsync();

		// Confirm deletion scoped to the active dialog
		var dialog = page.Locator("[role='dialog']");
		await dialog.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
		await dialog.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Slet" }).ClickAsync();
		await postCard.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
	}

	public ILocator GetPostWithContent(string content) =>
		PostCards.Filter(new LocatorFilterOptions { HasText = content }).First;

	public ILocator GetPostCards() => PostCards;
}
