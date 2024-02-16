using FluentAssertions;
using Jordnaer.Features.Category;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Jordnaer.Tests.Category;

[Trait("Category", "IntegrationTest")]
[Collection(nameof(JordnaerWebApplicationFactoryCollection))]
public class CategoryCache_Should
{
	private readonly JordnaerWebApplicationFactory _factory;

	public CategoryCache_Should(JordnaerWebApplicationFactory factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task Get_Categories_Successfully()
	{
		// Arrange
		using var scope = _factory.Services.CreateScope();
		var sut = scope.ServiceProvider.GetRequiredService<ICategoryCache>();

		// Act
		var response = await sut.GetOrCreateCategoriesAsync();

		// Assert
		response.Should().NotBeNullOrEmpty();
		response!.Count.Should().BeGreaterThan(0);
	}
}
