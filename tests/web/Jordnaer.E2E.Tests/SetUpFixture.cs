using Jordnaer.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests;

[SetUpFixture]
public class SetUpFixture
{
	private IPlaywright _playwright = null!;
	public static IBrowser Browser = null!;

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
	}

	[OneTimeTearDown]
	public async Task OneTimeTearDown()
	{
		await Browser.CloseAsync(new BrowserCloseOptions { Reason = "Test run finished." });
		await Browser.DisposeAsync();
		_playwright.Dispose();
	}
}