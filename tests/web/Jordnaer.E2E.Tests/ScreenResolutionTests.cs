using Jordnaer.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests;

[TestFixture]
[Category(nameof(TestCategory.UI))]
[Category(nameof(TestCategory.SkipInCi))]
[Parallelizable(ParallelScope.All)]
public class ScreenResolutionTests : BrowserTest
{
	[Test]
	[Description("This test will take screenshots of the landing page in different screen resolutions.")]
	[TestCase(360, 800)]
	[TestCase(390, 844)]
	[TestCase(393, 873)]
	[TestCase(412, 915)]
	[TestCase(414, 896)]
	[TestCase(640, 480)]
	[TestCase(800, 600)]
	[TestCase(1024, 768)]
	[TestCase(1280, 720)]
	[TestCase(1366, 768)]
	[TestCase(1440, 900)]
	[TestCase(1600, 900)]
	[TestCase(1680, 1050)]
	[TestCase(1920, 1080)]
	[TestCase(2560, 1440)]
	[TestCase(2560, 1600)]
	[TestCase(3840, 2160)]
	[TestCase(4096, 2160)]
	public async Task LandingPage(int width, int height)
	{
		var page = await SetUpFixture.Context.NewPageAsync();

		await page.SetViewportSizeAsync(width, height);
		await page.GotoAsync(TestConfiguration.Values.BaseUrl);
		await page.Locator("img#landing-page-center-image")
			.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

		var path = $"{Constants.ScreenshotFolder}/{width}x{height}.png";
		await page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
		Console.WriteLine($"Saved image to path '{Path.GetFullPath(path)}'");

		await page.CloseAsync();
	}
}
