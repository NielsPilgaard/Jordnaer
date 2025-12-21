using Jordnaer.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests.AuthenticatedTests;

[Parallelizable(ParallelScope.All)]
[TestFixture]
[Category(nameof(TestCategory.UI))]
[Category(nameof(TestCategory.Authenticated))]
[Category(nameof(TestCategory.SkipInCi))]
public class ChatTests : BrowserTest
{
	[Test]
	public async Task Chat_Search_Should_Return_Niels_When_Searching_For_Niels()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		var chatPage = page.CreateChatPage();

		await chatPage.NavigateAsync(TestConfiguration.Values.BaseUrl);
		await chatPage.SearchForUserAsync("Niels Pilgaard Grøndahl");

		await Expect(page.GetByText("Niels Pilgaard Grøndahl").First).ToBeVisibleAsync();

		await page.CloseAsync();
	}

	[Test]
	public async Task Chat_Should_Be_Able_To_Send_Messages()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		var chatPage = page.CreateChatPage();

		await chatPage.NavigateAsync(TestConfiguration.Values.BaseUrl);
		await chatPage.SearchForUserAsync("Niels Pilgaard Grøndahl");
		await chatPage.SelectUserFromSearchResultsAsync("Niels Pilgaard Grøndahl");

		await chatPage.SendMessageAsync("Dette er en test meddelelse.");

		await Expect(chatPage.GetMessage("Dette er en test meddelelse.")).ToBeVisibleAsync();

		await page.CloseAsync();
	}

	[Test]
	public async Task Chat_Should_Clear_The_Input_After_Message_Is_Sent()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		var chatPage = page.CreateChatPage();

		await chatPage.NavigateAsync(TestConfiguration.Values.BaseUrl);
		await chatPage.SearchForUserAsync("Niels Pilgaard Grøndahl");
		await chatPage.SelectUserFromSearchResultsAsync("Niels Pilgaard Grøndahl");

		await chatPage.SendMessageAsync("Dette er en test meddelelse.");

		await Expect(chatPage.GetMessageInput()).ToBeEmptyAsync();

		await page.CloseAsync();
	}

	[Test]
	public async Task Chat_Should_Hide_Footers()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		var chatPage = page.CreateChatPage();

		await chatPage.NavigateAsync(TestConfiguration.Values.BaseUrl);

		await Expect(chatPage.GetFooterLink("Kontakt")).ToBeHiddenAsync();

		await page.CloseAsync();
	}
}
