using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests;

[TestFixture]
[Category("UI")]
public class ScreenResolutionTests : PageTest
{
    [Test]
    [Parallelizable(ParallelScope.Self)]
    [Description("This test will take screenshots of the landing page in different screen resolutions.")]
    // List of resolutions as test cases
    [TestCase(640, 480)]
    [TestCase(800, 600)]
    [TestCase(1024, 768)]
    [TestCase(1280, 720)]
    [TestCase(1280, 1024)]
    [TestCase(1366, 768)]
    [TestCase(1440, 900)]
    [TestCase(1600, 900)]
    [TestCase(1680, 1050)]
    [TestCase(1920, 1080)]
    [TestCase(1920, 1200)]
    [TestCase(2560, 1440)]
    [TestCase(2560, 1600)]
    [TestCase(3840, 2160)]
    [TestCase(4096, 2160)]
    [TestCase(360, 800)]
    [TestCase(390, 844)]
    [TestCase(414, 896)]
    [TestCase(393, 873)]
    [TestCase(412, 915)]
    public async Task LandingPage(int width, int height)
    {
        await Page.SetViewportSizeAsync(width, height);
        await Page.GotoAsync("https://jordnaer.azurewebsites.net/");
        string imgSelector = "img[src*='nathan-dumlao-P5MXtYfgmmw-unsplash_cropped.webp']";
        await Page.Locator(imgSelector).WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        string path = $"screenshots/{width}x{height}.png";
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = path });
        Console.WriteLine($"Saved image to path '{Path.GetFullPath(path)}'");
    }
}
