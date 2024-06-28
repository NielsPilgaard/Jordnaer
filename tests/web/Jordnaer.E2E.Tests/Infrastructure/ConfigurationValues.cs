using Microsoft.Extensions.Configuration;

namespace Jordnaer.E2E.Tests.Infrastructure;

public class ConfigurationValues
{
	public ConfigurationValues()
	{
		var configurationBuilder = new ConfigurationBuilder()
								   .AddJsonFile("appsettings.json", optional: true)
								   .AddUserSecrets<ConfigurationValues>()
								   .AddEnvironmentVariables();

		IConfiguration configuration = configurationBuilder.Build();

		BaseUrl = (configuration["Playwright_BaseUrl"] ?? "https://localhost:7116").TrimEnd('/');
		_username = configuration["Playwright_Username"];
		_password = configuration["Playwright_Password"];
		Device = configuration["Playwright_Device"];
		Browser = configuration["Playwright_Browser"] ?? "Chromium";
		Headless = configuration["Playwright_Headless"]?
					   .Equals(bool.TrueString, StringComparison.InvariantCultureIgnoreCase)
				   ?? true;

		if (!float.TryParse(configuration["Playwright_SlowMo"], out var slowMo))
		{
			slowMo = 0;
		}

		SlowMo = slowMo;
	}

	public string Browser { get; }
	public string BaseUrl { get; }
	public string? Device { get; }
	public bool Headless { get; }
	public float? SlowMo { get; }

	private readonly string? _username;
	public string TestUserName => string.IsNullOrEmpty(_username)
									  ? throw new InvalidOperationException("Test user name is not set")
									  : _username;

	private readonly string? _password;

	public string TestUserPassword => string.IsNullOrEmpty(_password)
										  ? throw new InvalidOperationException("Test user password is not set")
										  : _password;
}