using Microsoft.Playwright;

namespace Jordnaer.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Profile page (/profile).
/// Encapsulates selectors and actions to reduce coupling between tests and UI structure.
/// </summary>
public class ProfilePage(IPage page)
{
	private const string PageUrl = "/profile";

	// Form fields
	private ILocator FirstNameField => page.GetByLabel("Fornavn");
	private ILocator LastNameField => page.GetByLabel("Efternavn");
	private ILocator UsernameField => page.GetByLabel("Brugernavn");
	private ILocator SaveButton => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Gem" }).First;

	// Profile picture upload
	private ILocator ProfilePictureInput => page.Locator("input[type='file']").First;

	// View profile link (shows current username)
	private ILocator ViewProfileButton => page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Se hvordan andre ser din profil" });

	public async Task NavigateAsync(string baseUrl)
	{
		await page.GotoAsync($"{baseUrl}{PageUrl}");
		await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
	}

	public async Task FillFirstNameAsync(string firstName)
	{
		await FirstNameField.ClearAsync();
		await FirstNameField.FillAsync(firstName);
	}

	public async Task FillLastNameAsync(string lastName)
	{
		await LastNameField.ClearAsync();
		await LastNameField.FillAsync(lastName);
	}

	public async Task SaveAsync()
	{
		await SaveButton.ClickAsync();
		await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
	}

	public async Task UpdateFirstNameAsync(string firstName)
	{
		await FillFirstNameAsync(firstName);
		await SaveAsync();
	}

	public async Task UploadProfilePictureAsync(string filePath)
	{
		await ProfilePictureInput.SetInputFilesAsync(filePath);
		await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
	}

	public ILocator GetFirstNameField() => FirstNameField;
	public ILocator GetLastNameField() => LastNameField;
	public ILocator GetUsernameField() => UsernameField;
	public ILocator GetSaveButton() => SaveButton;
	public ILocator GetViewProfileButton() => ViewProfileButton;
}
