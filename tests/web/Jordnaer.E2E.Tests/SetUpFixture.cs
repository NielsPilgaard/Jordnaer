using Jordnaer.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests;

[SetUpFixture]
public class SetUpFixture
{
	private IPlaywright _playwright = null!;

	/// <summary>
	/// Use this when you need to disable loading of authentication state, like when logging in.
	/// </summary>
	public static IBrowser Browser = null!;

	/// <summary>
	/// Use this for all tests that do not require control of the authentication state.
	/// </summary>
	public static IBrowserContext Context = null!;

	[OneTimeSetUp]
	public async Task GlobalSetup()
	{
		// Always run Playwright install, it stops if Playwright is already installed
		Program.Main(["install"]);

		_playwright = await Playwright.CreateAsync();

		Browser = await _playwright[TestConfiguration.Values.Browser].LaunchAsync(new BrowserTypeLaunchOptions
		{
			Headless = TestConfiguration.Values.Headless,
			SlowMo = TestConfiguration.Values.SlowMo
		});

		await Browser.Login(_playwright);

		Context = await NewContext();
	}

	private async Task<IBrowserContext> NewContext()
	{
		var newPageOptions = new BrowserNewContextOptions
		{
			StorageStatePath = "auth.json"
		};

		if (TestConfiguration.Values.Device is null)
		{
			return await Browser.NewContextAsync(newPageOptions);
		}

		var device = _playwright.Devices[TestConfiguration.Values.Device];

		newPageOptions.ViewportSize = device.ViewportSize;
		newPageOptions.UserAgent = device.UserAgent;

		return await Browser.NewContextAsync(newPageOptions);
	}

	[OneTimeTearDown]
	public async Task OneTimeTearDown()
	{
		await Context.CloseAsync(new BrowserContextCloseOptions { Reason = "Test run finished." });
		await Context.DisposeAsync();

		await Browser.CloseAsync(new BrowserCloseOptions { Reason = "Test run finished." });
		await Browser.DisposeAsync();

		_playwright.Dispose();
	}
}