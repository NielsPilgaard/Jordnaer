using FluentAssertions;
using Jordnaer.Client.Features.LookingFor;
using Refit;
using Xunit;

namespace Jordnaer.Server.Tests.LookingFor;

[Trait("Category", "IntegrationTest")]
[Collection(nameof(JordnaerWebApplicationFactoryCollection))]
public class LookingForApi_Should
{
    private readonly JordnaerWebApplicationFactory _factory;

    public LookingForApi_Should(JordnaerWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_LookingForList_Successfully()
    {
        // Arrange
        var client = RestService.For<ILookingForClient>(_factory.CreateClient());

        // Act
        var response = await client.GetLookingFor();

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Should().NotBeNullOrEmpty();
        response.Content!.Count.Should().BeGreaterThan(0);
    }
}
