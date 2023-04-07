using Bogus;
using FluentAssertions;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Jordnaer.Server.Tests.Authentication;

[Trait("Category", "IntegrationTest")]
public class UserApi_Should : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private const string VALID_PASSWORD = "123456789ABCabc";

    public UserApi_Should(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Delete_User_When_User_Is_Authenticated()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userInfo = new UserInfo { Email = new Faker().Internet.ExampleEmail(), Password = VALID_PASSWORD };

        // Register and login the user first
        await client.PostAsJsonAsync("/api/auth/register", userInfo);
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", userInfo);

        // Add the authentication cookie to the client
        string? authCookie = loginResponse.Headers.GetValues("Set-Cookie").FirstOrDefault();
        client.DefaultRequestHeaders.Add("Cookie", authCookie);

        // Act
        var response = await client.DeleteAsync("/api/user");

        // Assert
        response.Should().Be200Ok();
    }

    [Fact]
    public async Task Fail_To_Delete_User_When_User_Is_Not_Authenticated()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync("/api/user");

        // Assert
        response.Should().Be401Unauthorized();
    }
}
