using Bogus;
using FluentAssertions;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Jordnaer.Server.Tests;

[Trait("Category", "IntegrationTest")]
public class authapi_should : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private const string VALID_PASSWORD = "123456789ABCabc";

    public authapi_should(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task register_user_when_user_info_is_valid()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userInfo = new Faker<UserInfo>()
            .RuleFor(info => info.Email, faker => faker.Internet.Email())
            .RuleFor(info => info.Password, (_, info) => info.Password = VALID_PASSWORD)
            .Generate();

        // Act
        _testOutputHelper.WriteLine(
            $"Posting userInfo {JsonConvert.SerializeObject(userInfo)} as json to endpoint '/api/auth/register'");
        var response = await client.PostAsJsonAsync("/api/auth/register", userInfo);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Theory]
    [InlineData("aaaaaaaaaa")]
    [InlineData("aaaaaAAAAA")]
    [InlineData("aaaaa6789")]
    [InlineData("123456789")]
    [InlineData("12345AAAAA")]
    public async Task not_register_user_when_user_info_is_invalid(string invalidPassword)
    {
        // Arrange
        var client = _factory.CreateClient();
        var userInfo = new Faker<UserInfo>()
            .RuleFor(info => info.Email, faker => faker.Internet.Email())
            .RuleFor(info => info.Password, (_, info) => info.Password = invalidPassword)
            .Generate();

        // Act
        _testOutputHelper.WriteLine(
            $"Posting userInfo {JsonConvert.SerializeObject(userInfo)} as json to endpoint '/api/auth/register'");
        var response = await client.PostAsJsonAsync("/api/auth/register", userInfo);

        // Assert
        response.Should().Be401Unauthorized();
    }
}
