using Bogus;
using FluentAssertions;
using Jordnaer.Server.Database;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Jordnaer.Server.Tests.Authentication;

[Trait("Category", "IntegrationTest")]
[Collection(nameof(JordnaerServerFactoryCollection))]
public class UserApi_Should
{
    private readonly JordnaerWebApplicationFactory _factory;
    private const string VALID_PASSWORD = "123456789ABCabc";

    public UserApi_Should(JordnaerWebApplicationFactory factory)
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

        // Get the user id
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(userInfo.Email);
        string id = user!.Id;

        // Add the authentication cookie to the client
        string? authCookie = loginResponse.Headers.GetValues("Set-Cookie").FirstOrDefault();
        client.DefaultRequestHeaders.Add("Cookie", authCookie);

        // Act
        var response = await client.DeleteAsync($"/api/users/{id}");

        // Assert
        response.Should().Be200Ok();
    }

    [Fact]
    public async Task Fail_To_Delete_User_When_User_Is_Not_Authenticated()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.DeleteAsync($"/api/users/{Guid.NewGuid()}");

        // Assert
        response.Should().Be302Redirect();
    }
}
