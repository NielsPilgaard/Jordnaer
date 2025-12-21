using Microsoft.Playwright;

namespace Jordnaer.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Landing page (/)
/// Encapsulates selectors and actions to reduce coupling between tests and UI structure
/// </summary>
public class LandingPage
{
	private readonly IPage _page;
	private const string PageUrl = "/";

	public LandingPage(IPage page)
	{
		_page = page;
	}

	// Locators - Navigation
	private ILocator JoinLink => _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "VÆR' MED" }).First;
	private ILocator GroupsLink => _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "GRUPPER", Exact = true });
	private ILocator PostsLink => _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "OPSLAG" });
	private ILocator PeopleLink => _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Personer" });
	private ILocator LogoLink => _page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Logo" });

	// Locators - Footer
	public ILocator GetFooterLink(string linkText) =>
		_page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = linkText });

	// Locators - Images
	private ILocator CenterImage => _page.Locator("img#landing-page-center-image");

	// Actions
	public async Task NavigateAsync(string baseUrl)
	{
		await _page.GotoAsync($"{baseUrl}{PageUrl}");
		await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
	}

	public async Task ClickJoinAsync()
	{
		await JoinLink.ClickAsync();
	}

	public async Task ClickGroupsAsync()
	{
		await GroupsLink.ClickAsync();
	}

	public async Task ClickPostsAsync()
	{
		await PostsLink.ClickAsync();
	}

	// Getters for assertions
	public ILocator GetJoinLink() => JoinLink;
	public ILocator GetGroupsLink() => GroupsLink;
	public ILocator GetPostsLink() => PostsLink;
	public ILocator GetPeopleLink() => PeopleLink;
	public ILocator GetLogoLink() => LogoLink;
	public ILocator GetCenterImage() => CenterImage;
}
