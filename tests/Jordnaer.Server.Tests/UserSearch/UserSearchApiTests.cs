using System.Net.Http.Json;
using FluentAssertions;
using Jordnaer.Shared.UserSearch;
using Xunit;

namespace Jordnaer.Server.Tests.UserSearch;

[Trait("Category", "IntegrationTest")]
[Collection(nameof(JordnaerWebApplicationFactoryCollection))]
public class UserSearchApi_Should
{
    private readonly JordnaerWebApplicationFactory _factory;

    public UserSearchApi_Should(JordnaerWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // TODO: Test UserSearchApi - Rate limiting especially
    [Fact]
    public async Task Return_UserSearchResult_WhenCallIsValid()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetFromJsonAsync<UserSearchResult>("/api/users/search");

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task Return_429TooManyRequests_When_Too_Many_Requests_Are_Sent_By_The_Same_Client()
    {
        // Arrange
        var client = _factory.CreateClient();

        var tasks = new List<Task>();

        // Make at least 15 calls to hit the limit
        for (int i = 0; i < 25; i++)
        {
            tasks.Add(client.GetAsync("/api/users/search"));
        }

        await Task.WhenAll(tasks);

        // Act
        var rateLimitedResponse = await client.GetAsync("/api/users/search");

        // Assert
        rateLimitedResponse.Should().Be429TooManyRequests();
    }
}
