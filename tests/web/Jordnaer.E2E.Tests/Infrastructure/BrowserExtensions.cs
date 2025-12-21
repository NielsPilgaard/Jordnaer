using Microsoft.Playwright;

namespace Jordnaer.E2E.Tests.Infrastructure;

public static class BrowserExtensions
{
	private static readonly PageGetByPlaceholderOptions GetByPlaceholderOptions = new PageGetByPlaceholderOptions
	{ Exact = true };
	public static async Task Login(this IBrowser browser, IPlaywright playwright)
	{
		var page = await browser.NewPageAsync(playwright, false);

		// Use LoginPage Page Object for maintainability
		var loginPage = page.CreateLoginPage();
		await loginPage.NavigateAsync(TestConfiguration.Values.BaseUrl);
		await loginPage.LoginAsync(TestConfiguration.Values.TestUserName, TestConfiguration.Values.TestUserPassword);

		// Save authentication state
		await page.Context.StorageStateAsync(new BrowserContextStorageStateOptions
		{
			Path = "auth.json"
		});

		await page.CloseAsync();
	}

	public static async Task<IPage> NewPageAsync(this IBrowser browser, IPlaywright playwright, bool loadAuthenticationState = true)
	{
		var newPageOptions = new BrowserNewPageOptions();

		if (loadAuthenticationState)
		{
			newPageOptions.StorageStatePath = "auth.json";
		}

		if (TestConfiguration.Values.Device is null)
		{
			return await browser.NewPageAsync(newPageOptions);
		}

		var device = playwright.Devices[TestConfiguration.Values.Device];

		newPageOptions.ViewportSize = device.ViewportSize;
		newPageOptions.UserAgent = device.UserAgent;

		return await browser.NewPageAsync(newPageOptions);
	}
}