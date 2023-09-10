using System.Net;
using FluentAssertions;
using Jordnaer.Client.Features.UserSearch;
using Jordnaer.Shared;
using Refit;
using Xunit;

namespace Jordnaer.Server.Tests.UserSearch;

[Collection(nameof(JordnaerWebApplicationFactoryCollection))]
public class UserSearchApi_Should
{
    private readonly JordnaerWebApplicationFactory _factory;

    public UserSearchApi_Should(JordnaerWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "IntegrationTest")]
    public async Task Return_UserSearchResult_WhenCallIsValid()
    {
        // Arrange
        var client = RestService.For<IUserSearchClient>(_factory.CreateClient());

        // Act
        var response = await client.GetUsers(new UserSearchFilter());

        // Assert
        response.Should().NotBeNull();
    }

#pragma warning disable xUnit1004
    [Fact(Skip = "This test only works in isolation.")]
#pragma warning restore xUnit1004
    [Trait("Category", "ManualTest")]
    public async Task Return_429TooManyRequests_When_Too_Many_Requests_Are_Sent_By_The_Same_Client()
    {
        // Arrange
        var client = RestService.For<IUserSearchClient>(_factory.CreateClient());

        var tasks = new List<Task>();

        // Make at least 15 calls to hit the limit
        for (int i = 0; i < 25; i++)
        {
            tasks.Add(client.GetUsers(new UserSearchFilter()));
        }

        await Task.WhenAll(tasks);

        // Act
        var rateLimitedResponse = await client.GetUsers(new UserSearchFilter());

        // Assert
        rateLimitedResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
