using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace Jordnaer.E2E.Tests.Infrastructure;

public static class TestConfiguration
{
	private static readonly IConfiguration Configuration = new ConfigurationBuilder()
														   .AddJsonFile("appsettings.json", optional: true)
														   .AddUserSecrets<PlaywrightOptions>()
														   .AddEnvironmentVariables()
														   .Build();

	private static readonly Lazy<PlaywrightOptions> LazyOptions = new(() =>
	{
		var options = new PlaywrightOptions();
		Configuration!.GetSection(PlaywrightOptions.SectionName).Bind(options);
		return options;
	});

	public static readonly PlaywrightOptions Values = LazyOptions.Value;

}

public class PlaywrightOptions
{
	public const string SectionName = "Playwright";

	[Required(AllowEmptyStrings = false)]
	public  string BaseUrl { get; init; } = "https://localhost:7116";

	[Required(AllowEmptyStrings = false)]
	public string Username { get; init; } = null!;

	[Required(AllowEmptyStrings = false)]
	public string Password { get; init; } = null!;

	public bool Headless { get; init; } = true;
	public string Browser { get; init; } = "Chromium";
	public string? Device { get; init; }
	public float SlowMo { get; init; } = 0;
}