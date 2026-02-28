using Jordnaer.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Jordnaer.E2E.Tests;

[SetUpFixture]
public class SetUpFixture
{
	private IPlaywright _playwright = null!;
	private static E2eWebApplicationFactory _factory = null!;
	private string _authFilePath = null!;
	private string _authBFilePath = null!;

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

		BaseUrl = _factory.ServerAddress;

		// Always run Playwright install, it stops if Playwright is already installed
		var exitCode = Microsoft.Playwright.Program.Main(["install"]);
		if (exitCode != 0)
		{
			throw new Exception($"Playwright install failed with exit code {exitCode}. Check the output above for details.");
		}

		_playwright = await Playwright.CreateAsync();

		Browser = await _playwright[TestConfiguration.Values.Browser].LaunchAsync(new BrowserTypeLaunchOptions
		{
			Headless = TestConfiguration.Values.Headless,
			SlowMo = TestConfiguration.Values.SlowMo
		});

		var runId = Guid.NewGuid().ToString("N");
		_authFilePath = Path.Combine(Path.GetTempPath(), $"auth-{runId}.json");
		_authBFilePath = Path.Combine(Path.GetTempPath(), $"auth-b-{runId}.json");

		await Browser.Login(_playwright, BaseUrl, E2eWebApplicationFactory.UserAEmail, E2eWebApplicationFactory.UserAPassword, _authFilePath);
		await Browser.Login(_playwright, BaseUrl, E2eWebApplicationFactory.UserBEmail, E2eWebApplicationFactory.UserBPassword, _authBFilePath);

		Context = await NewContext(_authFilePath);
		ContextB = await NewContext(_authBFilePath);
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
			try { await ContextB.CloseAsync(new BrowserContextCloseOptions { Reason = "Test run finished." }); } catch (Exception ex) { Console.Error.WriteLine($"Error closing ContextB: {ex}"); }
			try { await ContextB.DisposeAsync(); } catch (Exception ex) { Console.Error.WriteLine($"Error disposing ContextB: {ex}"); }
		}

		if (Context is not null)
		{
			try { await Context.CloseAsync(new BrowserContextCloseOptions { Reason = "Test run finished." }); } catch (Exception ex) { Console.Error.WriteLine($"Error closing Context: {ex}"); }
			try { await Context.DisposeAsync(); } catch (Exception ex) { Console.Error.WriteLine($"Error disposing Context: {ex}"); }
		}

		if (Browser is not null)
		{
			try { await Browser.CloseAsync(new BrowserCloseOptions { Reason = "Test run finished." }); } catch (Exception ex) { Console.Error.WriteLine($"Error closing Browser: {ex}"); }
			try { await Browser.DisposeAsync(); } catch (Exception ex) { Console.Error.WriteLine($"Error disposing Browser: {ex}"); }
		}

		try { _playwright?.Dispose(); } catch (Exception ex) { Console.Error.WriteLine($"Error disposing Playwright: {ex}"); }

		if (_factory is not null)
		{
			try { await _factory.DisposeAsync(); } catch (Exception ex) { Console.Error.WriteLine($"Error disposing factory: {ex}"); }
		}

		if (_authFilePath is not null && File.Exists(_authFilePath))
		{
			try { File.Delete(_authFilePath); } catch (Exception ex) { Console.Error.WriteLine($"Error deleting auth file '{_authFilePath}': {ex}"); }
		}

		if (_authBFilePath is not null && File.Exists(_authBFilePath))
		{
			try { File.Delete(_authBFilePath); } catch (Exception ex) { Console.Error.WriteLine($"Error deleting auth-b file '{_authBFilePath}': {ex}"); }
		}
	}
}
