using FluentAssertions;
using Jordnaer.Features.Category;
using Jordnaer.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Jordnaer.Tests.Category;

[Trait("Category", "IntegrationTest")]
[Collection(nameof(JordnaerWebApplicationFactoryCollection))]
public class CategoryCacheTests(JordnaerWebApplicationFactory factory)
{
	[Fact]
	public async Task Get_Categories_Successfully()
	{
		// Arrange
		using var scope = factory.Services.CreateScope();
		var sut = scope.ServiceProvider.GetRequiredService<ICategoryCache>();

		// Act
		var response = await sut.GetOrCreateCategoriesAsync();

		// Assert
		response.Should().NotBeNullOrEmpty();
		response.Count.Should().BeGreaterThan(0);
	}
}
