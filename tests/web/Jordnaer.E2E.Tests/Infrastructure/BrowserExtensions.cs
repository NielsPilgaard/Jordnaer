using Microsoft.Playwright;

namespace Jordnaer.E2E.Tests.Infrastructure;

public static class BrowserExtensions
{
	private static readonly PageGetByPlaceholderOptions GetByPlaceholderOptions = new PageGetByPlaceholderOptions
	{ Exact = true };
	public static async Task Login(this IBrowser browser, IPlaywright playwright)
	{
		var page = await browser.NewPageAsync(playwright, false);
		await page.GotoAsync(TestConfiguration.Values.BaseUrl);
		await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Opret ny konto" })
				  .ClickAsync();

		await page.GetByText("Log ind med eksisterende konto").ClickAsync();
		await page.GetByPlaceholder("navn@eksempel.com", GetByPlaceholderOptions).ClickAsync();
		await page.GetByPlaceholder("navn@eksempel.com", GetByPlaceholderOptions).FillAsync(TestConfiguration.Values.TestUserName);
		await page.GetByPlaceholder("Adgangskode", GetByPlaceholderOptions).FillAsync(TestConfiguration.Values.TestUserPassword);

		await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Log ind", Exact = true })
				  .ClickAsync();

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