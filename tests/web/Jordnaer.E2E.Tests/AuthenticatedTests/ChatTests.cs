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
	public async Task Chat_Search_Should_Return_No_Results_When_Searching_For_Random_String()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		await page.GotoAsync(TestConfiguration.Values.BaseUrl + "/chat");

		// Search for user
		await Expect(page.Locator("div").Filter(new LocatorFilterOptions
		{
			HasText = "Søg efter bruger"
		}).Nth(2)).ToBeVisibleAsync();
		await page.GetByRole(AriaRole.Textbox).ClickAsync();
		await page.GetByRole(AriaRole.Textbox).FillAsync(Guid.NewGuid().ToString());
		await Expect(page.GetByText("Ingen brugere fundet")).ToBeVisibleAsync();

		await page.CloseAsync();
	}

	[Test]
	public async Task Chat_Search_Should_Return_Niels_When_Searching_For_Niels()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		await page.GotoAsync(TestConfiguration.Values.BaseUrl + "/chat");

		// Search for user
		await page.GetByRole(AriaRole.Textbox).ClickAsync();
		await page.GetByRole(AriaRole.Textbox).FillAsync("Niels Pilgaard Grøndahl");
		await Expect(page.GetByText("Niels Pilgaard Grøndahl")).ToBeVisibleAsync();

		await page.CloseAsync();
	}

	[Test]
	public async Task Chat_Should_Be_Able_To_Send_Messages()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		await page.GotoAsync(TestConfiguration.Values.BaseUrl + "/chat");

		// Search for user
		await page.GetByRole(AriaRole.Textbox).ClickAsync();
		await page.GetByRole(AriaRole.Textbox).FillAsync("Niels Pilgaard Grøndahl");
		await Task.Delay(500);
		await page.GetByText("Niels Pilgaard Grøndahl").First.ClickAsync();

		// Dismiss cookie banner
		await page.GetByText("Mini Møder anvender cookies").ClickAsync();

		// Send message
		await page.Locator("#chat-message-input").ClickAsync();
		await page.Locator("#chat-message-input").FillAsync("Dette er en test meddelelse.");
		await page.GetByLabel("Icon Button").ClickAsync();

		// Assert message was sent
		await Expect(page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions
		{
			Name = "Dette er en test meddelelse."
		}).First).ToBeVisibleAsync();

		await page.CloseAsync();
	}

	[Test]
	public async Task Chat_Should_Clear_The_Input_After_Message_Is_Sent()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		await page.GotoAsync(TestConfiguration.Values.BaseUrl + "/chat");

		// Search for user
		await page.GetByRole(AriaRole.Textbox).ClickAsync();
		await page.GetByRole(AriaRole.Textbox).FillAsync("Niels Pilgaard Grøndahl");
		await Task.Delay(500);
		await page.GetByText("Niels Pilgaard Grøndahl").First.ClickAsync();

		// Dismiss cookie banner
		await page.GetByText("Mini Møder anvender cookies").ClickAsync();

		// Send message
		await page.Locator("#chat-message-input").ClickAsync();
		await page.Locator("#chat-message-input").FillAsync("Dette er en test meddelelse.");
		await page.GetByLabel("Icon Button").ClickAsync();

		// Assert message input is now empty
		await Expect(page.Locator("#chat-message-input")).ToBeEmptyAsync();

		await page.CloseAsync();
	}

	[Test]
	public async Task Chat_Should_Hide_Footers()
	{
		var page = await SetUpFixture.Context.NewPageAsync();
		await page.GotoAsync(TestConfiguration.Values.BaseUrl + "/chat");

		await Expect(page.GetByRole(AriaRole.Link, new PageGetByRoleOptions
		{
			Name = "Kontakt"
		})).ToBeHiddenAsync();

		await page.CloseAsync();
	}
}