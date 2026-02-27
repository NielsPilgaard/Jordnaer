using Jordnaer.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests;

[SetUpFixture]
public class SetUpFixture
{
	private IPlaywright _playwright = null!;
	private static E2eWebApplicationFactory _factory = null!;

	/// <summary>
	/// The DI service provider of the in-process test server.
	/// </summary>
	public static IServiceProvider Services => _factory.Services;

	/// <summary>
	/// The base URL of the in-process test server. Set during global setup.
	/// </summary>
	public static string BaseUrl { get; private set; } = null!;

	/// <summary>
	/// Use this when you need to disable loading of authentication state, like when logging in.
	/// </summary>
	public static IBrowser Browser = null!;

	/// <summary>
	/// Use this for authenticated tests running as User A.
	/// </summary>
	public static IBrowserContext Context = null!;

	/// <summary>
	/// Use this for multi-user tests that need a second authenticated user (User B).
	/// </summary>
	public static IBrowserContext ContextB = null!;

	[OneTimeSetUp]
	public async Task GlobalSetup()
	{
		_factory = new E2eWebApplicationFactory();
		await _factory.InitializeAsync();

		BaseUrl = _factory.Server.BaseAddress.ToString().TrimEnd('/');

		// Always run Playwright install, it stops if Playwright is already installed
		Microsoft.Playwright.Program.Main(["install"]);

		_playwright = await Playwright.CreateAsync();

		Browser = await _playwright[TestConfiguration.Values.Browser].LaunchAsync(new BrowserTypeLaunchOptions
		{
			Headless = TestConfiguration.Values.Headless,
			SlowMo = TestConfiguration.Values.SlowMo
		});

		await Browser.Login(_playwright, BaseUrl, E2eWebApplicationFactory.UserAEmail, E2eWebApplicationFactory.UserAPassword, "auth.json");
		await Browser.Login(_playwright, BaseUrl, E2eWebApplicationFactory.UserBEmail, E2eWebApplicationFactory.UserBPassword, "auth-b.json");

		Context = await NewContext("auth.json");
		ContextB = await NewContext("auth-b.json");
	}

	private async Task<IBrowserContext> NewContext(string storageStatePath)
	{
		var newPageOptions = new BrowserNewContextOptions
		{
			StorageStatePath = storageStatePath
		};

		if (TestConfiguration.Values.Device is null)
		{
			return await Browser.NewContextAsync(newPageOptions);
		}

		var device = _playwright.Devices[TestConfiguration.Values.Device];

		newPageOptions.ViewportSize = device.ViewportSize;
		newPageOptions.UserAgent = device.UserAgent;

		return await Browser.NewContextAsync(newPageOptions);
	}

	[OneTimeTearDown]
	public async Task OneTimeTearDown()
	{
		if (ContextB is not null)
		{
			try { await ContextB.CloseAsync(new BrowserContextCloseOptions { Reason = "Test run finished." }); } catch { }
			try { await ContextB.DisposeAsync(); } catch { }
		}

		if (Context is not null)
		{
			try { await Context.CloseAsync(new BrowserContextCloseOptions { Reason = "Test run finished." }); } catch { }
			try { await Context.DisposeAsync(); } catch { }
		}

		if (Browser is not null)
		{
			try { await Browser.CloseAsync(new BrowserCloseOptions { Reason = "Test run finished." }); } catch { }
			try { await Browser.DisposeAsync(); } catch { }
		}

		try { _playwright?.Dispose(); } catch { }

		if (_factory is not null)
		{
			try { await _factory.DisposeAsync(); } catch { }
		}
	}
}
