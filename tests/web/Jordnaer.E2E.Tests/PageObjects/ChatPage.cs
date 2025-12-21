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
	private ILocator SearchTextbox => _page.GetByRole(AriaRole.Textbox).First;
	private ILocator MessageInput => _page.Locator("#chat-message-input");
	private ILocator SendButton => _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Send" });

	// Actions
	public async Task NavigateAsync(string baseUrl)
	{
		await _page.GotoAsync($"{baseUrl}{PageUrl}");
		await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
	}

	public async Task SearchForUserAsync(string userName)
	{
		await SearchTextbox.FillAsync(userName);
	}

	public async Task SelectUserFromSearchResultsAsync(string userName)
	{
		var searchResult = _page.GetByText(userName).First;
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
