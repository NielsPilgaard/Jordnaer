using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("UITest")]
public partial class LandingPageTests : PageTest
{
    [GeneratedRegex(".*/auth/login")]
    private partial Regex LoginRegex();

    [GeneratedRegex(".*/posts")]
    private partial Regex PostsRegex();

    [GeneratedRegex(".*/groups")]
    private partial Regex GroupsRegex();

    [Test]
    public async Task When_User_Clicks_Join_User_Should_Be_Redirected_To_Login()
    {
        await Page.GotoAsync("https://jordnaer.azurewebsites.net/");

        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "VÆR' MED" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(LoginRegex());
    }

    [Test]
    public async Task When_User_Clicks_Posts_User_Should_Be_Redirected_To_Posts()
    {
        await Page.GotoAsync("https://jordnaer.azurewebsites.net/");

        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "OPSLAG" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(PostsRegex());
    }

    [Test]
    public async Task When_User_Clicks_Groups_User_Should_Be_Redirected_To_Groups()
    {
        await Page.GotoAsync("https://jordnaer.azurewebsites.net/");

        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "GRUPPER", Exact = true}).ClickAsync();

        await Expect(Page).ToHaveURLAsync(GroupsRegex());
    }
}
