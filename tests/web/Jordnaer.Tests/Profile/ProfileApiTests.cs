using System.Net;
using System.Net.Http.Json;
using Bogus;
using FluentAssertions;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Jordnaer.Server.Tests.Profile;

[Trait("Category", "IntegrationTest")]
[Collection(nameof(JordnaerWebApplicationFactoryCollection))]
public class ProfileApi_Should
{
    private readonly JordnaerWebApplicationFactory _factory;
    private const string Valid_Password = "123456789ABCabc";
    private readonly Faker _faker = new();

    public ProfileApi_Should(JordnaerWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task RegisterAndLoginTestUserAsync(HttpClient client)
    {
        var testUser = new UserInfo
        {
            Email = _faker.Internet.Email(),
            Password = Valid_Password
        };

        await client.PostAsJsonAsync("/api/auth/register", testUser);
        await client.PostAsJsonAsync("/api/auth/login", testUser);
    }

    [Fact]
    public async Task Get_Current_User_Profile()
    {
        // Arrange
        var client = _factory.CreateClient();

        var testUser = new UserInfo
        {
            Email = _faker.Internet.Email(),
            Password = Valid_Password
        };

        await client.PostAsJsonAsync("/api/auth/register", testUser);
        await client.PostAsJsonAsync("/api/auth/login", testUser);

        // Act
        var response = await client.GetAsync("/api/profiles");

        // Assert
        response.Should().Be200Ok();
    }

    [Fact]
    public async Task Update_Current_User_Profile_Successfully()
    {
        // Arrange
        var client = _factory.CreateClient();
        await RegisterAndLoginTestUserAsync(client);
        string newUserName = _faker.Internet.UserName();
        var userProfileToUpdate = await client.GetFromJsonAsync<UserProfile>("/api/profiles");
        userProfileToUpdate!.UserName = newUserName;

        // Act
        var response = await client.PutAsJsonAsync("/api/profiles", userProfileToUpdate);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var updatedUserProfile = await client.GetFromJsonAsync<UserProfile>("/api/profiles");
        updatedUserProfile!.UserName.Should().Be(newUserName);
    }

    [Fact]
    public async Task Update_Current_User_Profile_Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        var response = await client.PutAsJsonAsync("/api/profiles", new UserProfile { Id = Guid.NewGuid().ToString() });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }
}
