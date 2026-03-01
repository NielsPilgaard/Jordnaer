using Microsoft.Playwright;

namespace Jordnaer.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Groups area (/groups).
/// Encapsulates selectors and actions to reduce coupling between tests and UI structure.
/// </summary>
public class GroupPage(IPage page)
{
	private const string MyGroupsUrl = "/groups/my-groups";
	private const string CreateGroupUrl = "/groups/create";

	// Create group form
	private ILocator GroupNameField => page.GetByLabel("Gruppenavn");
	private ILocator ShortDescriptionField => page.GetByLabel("Kort beskrivelse");
	private ILocator CreateGroupButton => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Opret Gruppe" });

	public Task NavigateToMyGroupsAsync(string baseUrl) =>
		NavigateToAsync(baseUrl, MyGroupsUrl);

	public Task NavigateToCreateGroupAsync(string baseUrl) =>
		NavigateToAsync(baseUrl, CreateGroupUrl);

	private async Task NavigateToAsync(string baseUrl, string route)
	{
		await page.GotoAsync($"{baseUrl}{route}");
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

	public ILocator GetGroupByName(string groupName) =>
		page.GetByText(groupName).First;
}
