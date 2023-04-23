using Bogus;
using FluentAssertions;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Jordnaer.Server.Tests;

[Trait("Category", "IntegrationTest")]
[Collection(nameof(JordnaerServerFactory))]
public class AuthApi_Should
{
    private readonly WebApplicationFactory<Program> _factory;
    private const string VALID_PASSWORD = "123456789ABCabc";

    public AuthApi_Should(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_User_When_User_Info_Is_Valid()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userInfo = new Faker<UserInfo>()
            .RuleFor(info => info.Email, faker => faker.Internet.Email())
            .RuleFor(info => info.Password, (_, info) => info.Password = VALID_PASSWORD)
            .Generate();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", userInfo);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Theory]
    [InlineData("aaaaaaaaaa")]
    [InlineData("aaaaaAAAAA")]
    [InlineData("123456789")]
    [InlineData("12345AAAAA")]
    public async Task Fail_To_Register_User_When_User_Info_Is_Invalid(string invalidPassword)
    {
        // Arrange
        var client = _factory.CreateClient();
        var userInfo = new UserInfo { Email = new Faker().Internet.ExampleEmail(), Password = invalidPassword };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", userInfo);

        // Assert
        response.Should().Be401Unauthorized();
    }

    [Fact]
    public async Task Fail_To_Register_User_When_User_Is_Already_Registered()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userInfo = new UserInfo { Email = new Faker().Internet.ExampleEmail(), Password = VALID_PASSWORD };

        // Register the user once
        await client.PostAsJsonAsync("/api/auth/register", userInfo);

        // Act
        // Attempt to register the user again with the same email
        var response = await client.PostAsJsonAsync("/api/auth/register", userInfo);

        // Assert
        response.Should().Be401Unauthorized();
    }

    [Fact]
    public async Task Login_User_When_User_Info_Is_Valid()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userInfo = new UserInfo { Email = new Faker().Internet.ExampleEmail(), Password = VALID_PASSWORD };

        // Register the user first
        await client.PostAsJsonAsync("/api/auth/register", userInfo);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", userInfo);

        // Assert
        response.Should().Be200Ok();
    }

    [Fact]
    public async Task Fail_To_Login_User_When_User_Info_Is_Not_Registered()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userInfo = new UserInfo { Email = new Faker().Internet.ExampleEmail(), Password = VALID_PASSWORD };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", userInfo);

        // Assert
        response.Should().Be401Unauthorized();
    }

    [Fact]
    public async Task Logout_User_When_User_Is_Authenticated()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userInfo = new UserInfo { Email = new Faker().Internet.ExampleEmail(), Password = VALID_PASSWORD };

        // Register and login the user first
        await client.PostAsJsonAsync("/api/auth/register", userInfo);
        await client.PostAsJsonAsync("/api/auth/login", userInfo);

        // Act
        var response = await client.PostAsync("/api/auth/logout", null);

        // Assert
        response.Should().Be200Ok();
    }

    [Fact]
    public async Task Fail_To_Logout_User_When_User_Is_Not_Logged_In_And_Redict_To_Login()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.PostAsync("/api/auth/logout", null);

        // Assert
        response.Should().Be302Redirect();
    }
}
