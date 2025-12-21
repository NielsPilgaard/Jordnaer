using Microsoft.Playwright;

namespace Jordnaer.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Login page (/Account/Login)
/// Encapsulates selectors and actions to reduce coupling between tests and UI structure
/// </summary>
public class LoginPage
{
	private readonly IPage _page;
	private const string PageUrl = "/Account/Login";

	public LoginPage(IPage page)
	{
		_page = page;
	}

	// Locators
	private ILocator EmailInput => _page.GetByPlaceholder("navn@eksempel.com", new PageGetByPlaceholderOptions { Exact = true });
	private ILocator PasswordInput => _page.GetByPlaceholder("adgangskode", new PageGetByPlaceholderOptions { Exact = true });
	private ILocator LoginButton => _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Log ind", Exact = true });
	private ILocator CreateAccountLink => _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Opret konto" });
	private ILocator ForgotPasswordLink => _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Glemt" });

	// Actions
	public async Task NavigateAsync(string baseUrl)
	{
		await _page.GotoAsync($"{baseUrl}{PageUrl}");
		await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
	}

	public async Task FillEmailAsync(string email)
	{
		await EmailInput.FillAsync(email);
	}

	public async Task FillPasswordAsync(string password)
	{
		await PasswordInput.FillAsync(password);
	}

	public async Task ClickLoginAsync()
	{
		await LoginButton.ClickAsync();
		await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
	}

	public async Task LoginAsync(string email, string password)
	{
		await FillEmailAsync(email);
		await FillPasswordAsync(password);
		await ClickLoginAsync();
	}

	// Assertions helpers
	public ILocator GetCreateAccountLink() => CreateAccountLink;
	public ILocator GetForgotPasswordLink() => ForgotPasswordLink;
}
