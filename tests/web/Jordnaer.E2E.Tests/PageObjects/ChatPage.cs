using Microsoft.Playwright;

namespace Jordnaer.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Chat page (/chat)
/// Encapsulates selectors and actions to reduce coupling between tests and UI structure
/// </summary>
public class ChatPage
{
	private readonly IPage _page;
	private const string PageUrl = "/chat";

	public ChatPage(IPage page)
	{
		_page = page;
	}

	// Locators
	private ILocator SearchTextbox => _page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Start samtale med..." });
	private ILocator MessageInput => _page.Locator("#chat-message-input");
	// The send button is an adornment icon button on the text field - we target it by the parent container
	private ILocator SendButton => _page.Locator("#chat-message-input").Locator("..").GetByRole(AriaRole.Button).Last;

	// Actions
	public async Task NavigateAsync(string baseUrl)
	{
		await _page.GotoAsync($"{baseUrl}{PageUrl}");
		await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		// Wait for the MudLoading overlay to disappear - the chat page loads data asynchronously
		// after Blazor interactive rendering initializes, so NetworkIdle alone is insufficient.
		var loadingOverlay = _page.Locator(".mud-overlay");
		try
		{
			await loadingOverlay.WaitForAsync(new LocatorWaitForOptions
			{
				State = WaitForSelectorState.Visible,
				Timeout = 3_000
			});
			// Overlay appeared - now wait for it to go away
			await loadingOverlay.WaitForAsync(new LocatorWaitForOptions
			{
				State = WaitForSelectorState.Hidden,
				Timeout = 15_000
			});
		}
		catch
		{
			// Overlay may not appear at all if data loads fast enough
		}

		// Ensure the search box is interactive before returning
		await SearchTextbox.WaitForAsync(new LocatorWaitForOptions
		{
			State = WaitForSelectorState.Visible,
			Timeout = 15_000
		});
	}

	public async Task SearchForUserAsync(string userName)
	{
		await SearchTextbox.FillAsync(userName);
	}

	public async Task SelectUserFromSearchResultsAsync(string userName)
	{
		// Target the clickable MudListItem that contains the user name, not the inner text node.
		// The inner <p> element is intercepted by an overlay group div in the MudBlazor autocomplete popover.
		var searchResult = _page.Locator(".mud-list-item-clickable", new PageLocatorOptions { HasText = userName }).First;
		await searchResult.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
		await searchResult.ClickAsync();

		// Wait for chat conversation to load
		await MessageInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
	}

	public async Task SendMessageAsync(string message)
	{
		await MessageInput.FillAsync(message);
		await SendButton.ClickAsync();
	}

	public ILocator GetMessage(string messageText)
	{
		return _page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = messageText }).First;
	}

	public ILocator GetMessageInput() => MessageInput;

	public ILocator GetFooterLink(string linkName)
	{
		return _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = linkName });
	}
}
