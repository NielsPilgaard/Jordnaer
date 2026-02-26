using Microsoft.Playwright;

namespace Jordnaer.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Groups area (/groups).
/// Encapsulates selectors and actions to reduce coupling between tests and UI structure.
/// </summary>
public class GroupPage(IPage page)
{
	private const string GroupsUrl = "/groups";
	private const string CreateGroupUrl = "/groups/create";

	// Create group form
	private ILocator GroupNameField => page.GetByLabel("Gruppenavn");
	private ILocator ShortDescriptionField => page.GetByLabel("Kort beskrivelse");
	private ILocator CreateGroupButton => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Opret Gruppe" });

	// Search
	private ILocator SearchInput => page.GetByPlaceholder("Gruppenavn");

	// Group cards in search results
	private ILocator GroupCards => page.Locator(".group-card");

	public async Task NavigateToGroupsAsync(string baseUrl)
	{
		await page.GotoAsync($"{baseUrl}{GroupsUrl}");
		await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
	}

	public async Task NavigateToCreateGroupAsync(string baseUrl)
	{
		await page.GotoAsync($"{baseUrl}{CreateGroupUrl}");
		await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
	}

	public async Task FillGroupNameAsync(string name)
	{
		await GroupNameField.FillAsync(name);
	}

	public async Task FillShortDescriptionAsync(string description)
	{
		await ShortDescriptionField.FillAsync(description);
	}

	public async Task SubmitCreateGroupAsync()
	{
		await CreateGroupButton.ClickAsync();
		await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
	}

	public async Task CreateGroupAsync(string name, string description)
	{
		await FillGroupNameAsync(name);
		await FillShortDescriptionAsync(description);
		await SubmitCreateGroupAsync();
	}

	public async Task SearchForGroupAsync(string name)
	{
		await SearchInput.FillAsync(name);
		await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
	}

	public ILocator GetGroupCard(string groupName) =>
		page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = groupName });

	public ILocator GetGroupCards() => GroupCards;
	public ILocator GetSearchInput() => SearchInput;
}
