using Microsoft.Extensions.Configuration;

namespace Jordnaer.E2E.Tests.Infrastructure;

public static class TestConfiguration
{
	// ReSharper disable once InconsistentNaming
	private static readonly Lazy<ConfigurationValues> _configuration = new(() => new ConfigurationValues());
	public static readonly ConfigurationValues Values = _configuration.Value;
}

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
		TestUserName = configuration["Playwright_Username"];
		TestUserPassword = configuration["Playwright_Password"];
		Device = configuration["Playwright_Device"];
		Browser = configuration["Playwright_Browser"] ?? "Chromium";
		Headless = configuration["Playwright_Headless"]?.Equals(bool.TrueString, StringComparison.InvariantCultureIgnoreCase) ?? true;

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

	public string TestUserName => string.IsNullOrEmpty(field)
									  ? throw new InvalidOperationException("Test user name is not set")
									  : field;

	public string TestUserPassword => string.IsNullOrEmpty(field)
										  ? throw new InvalidOperationException("Test user password is not set")
										  : field;
}