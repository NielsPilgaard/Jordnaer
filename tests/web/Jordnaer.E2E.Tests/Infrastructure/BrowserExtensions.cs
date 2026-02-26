using Microsoft.Playwright;

namespace Jordnaer.E2E.Tests.Infrastructure;

public static class BrowserExtensions
{
	public static async Task Login(
		this IBrowser browser,
		IPlaywright playwright,
		string baseUrl,
		string email,
		string password,
		string storageStatePath)
	{
		var page = await browser.NewPageAsync(playwright, loadAuthenticationState: false);

		try
		{
			var loginPage = page.CreateLoginPage();
			await loginPage.NavigateAsync(baseUrl);
			await loginPage.LoginAsync(email, password);

			await page.Context.StorageStateAsync(new BrowserContextStorageStateOptions
			{
				Path = storageStatePath
			});
		}
		finally
		{
			await page.CloseAsync();
		}
	}

	public static async Task<IPage> NewPageAsync(
		this IBrowser browser,
		IPlaywright playwright,
		bool loadAuthenticationState = true,
		string storageStatePath = "auth.json")
	{
		var newPageOptions = new BrowserNewPageOptions();

		if (loadAuthenticationState)
		{
			newPageOptions.StorageStatePath = storageStatePath;
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
