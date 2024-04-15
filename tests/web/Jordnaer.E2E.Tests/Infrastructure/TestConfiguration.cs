namespace Jordnaer.E2E.Tests.Infrastructure;

public static class TestConfiguration
{
	// ReSharper disable once InconsistentNaming
	private static readonly Lazy<ConfigurationValues> _configuration = new(() => new ConfigurationValues());
	public static readonly ConfigurationValues Values = _configuration.Value;
}