namespace Jordnaer.E2E.Tests;

public static class Constants
{
	public static readonly string MainUrl = Environment.GetEnvironmentVariable("BaseUrl") ??
											"https://mini-moeder.dk";

	public const string ScreenshotFolder = "screenshots";

	public const string TestCategory = "UITest";
}
