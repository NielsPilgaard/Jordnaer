using Jordnaer.E2E.Tests.PageObjects;
using Microsoft.Playwright;

namespace Jordnaer.E2E.Tests.Infrastructure;

/// <summary>
/// Helper methods to create Page Objects with standard configuration
/// </summary>
public static class PageObjectHelpers
{
	public static LoginPage CreateLoginPage(this IPage page) => new(page);
	public static ChatPage CreateChatPage(this IPage page) => new(page);
	public static LandingPage CreateLandingPage(this IPage page) => new(page);
}
