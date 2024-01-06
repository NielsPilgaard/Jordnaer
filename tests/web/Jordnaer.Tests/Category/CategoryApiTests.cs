using FluentAssertions;
using Jordnaer.Features.Category;
using Refit;
using Xunit;

namespace Jordnaer.Server.Tests.Category;

[Trait("Category", "IntegrationTest")]
[Collection(nameof(JordnaerWebApplicationFactoryCollection))]
public class CategoryApi_Should
{
	private readonly JordnaerWebApplicationFactory _factory;

	public CategoryApi_Should(JordnaerWebApplicationFactory factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task Get_Categories_Successfully()
	{
		// Arrange
		var client = RestService.For<ICategoryClient>(_factory.CreateClient());

		// Act
		var response = await client.GetCategories();

		// Assert
		response.IsSuccessStatusCode.Should().BeTrue();
		response.Content.Should().NotBeNullOrEmpty();
		response.Content!.Count.Should().BeGreaterThan(0);
	}
}
