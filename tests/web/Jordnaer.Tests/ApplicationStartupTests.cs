using FluentAssertions;
using Jordnaer.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Jordnaer.Tests;

[Trait("Category", "IntegrationTest")]
[Collection(nameof(JordnaerWebApplicationFactoryCollection))]
public class ApplicationStartupTests
{
	private readonly JordnaerWebApplicationFactory _factory;

	public ApplicationStartupTests(JordnaerWebApplicationFactory factory)
	{
		_factory = factory;
	}

	[Fact]
	public void Application_CanStart_Successfully()
	{
		// Act - Creating the factory should build the application
		var act = () => _factory.Services;

		// Assert - No exceptions should be thrown during startup
		act.Should().NotThrow("the application should start without errors");
		_factory.Services.Should().NotBeNull("the service provider should be initialized");
	}

	[Fact]
	public async Task Application_StaticAssets_AreAccessible()
	{
		// Arrange
		using var client = _factory.CreateClient();

		// Act
		var response = await client.GetAsync("/favicon.ico");

		// Assert - We don't care if the file exists, just that the static file middleware is working
		// A 404 is acceptable, but we shouldn't get server errors
		((int)response.StatusCode).Should().BeLessThan(500,
			"static file middleware should be configured correctly");
	}
}
