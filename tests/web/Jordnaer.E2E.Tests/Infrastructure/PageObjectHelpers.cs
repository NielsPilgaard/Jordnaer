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
	public static TopBarPage CreateTopBarPage(this IPage page) => new(page);
	public static NotificationsPage CreateNotificationsPage(this IPage page) => new(page);
	public static GroupPage CreateGroupPage(this IPage page) => new(page);
	public static ProfilePage CreateProfilePage(this IPage page) => new(page);
	public static PostPage CreatePostPage(this IPage page) => new(page);
}
