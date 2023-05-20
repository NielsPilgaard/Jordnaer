using FluentAssertions;
using Jordnaer.Server.Database;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Jordnaer.Server.Tests;

[Trait("Category", "IntegrationTest")]
[Collection(nameof(JordnaerServerFactoryCollection))]
public class AzureAppConfiguration_Should
{
    private readonly JordnaerWebApplicationFactory _factory;

    public AzureAppConfiguration_Should(JordnaerWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Contain_Authentication_Scheme_Facebook()
    {
        // Arrange
        var facebookOptions = new FacebookOptions();
        var configuration = _factory.Services.GetRequiredService<IConfiguration>();

        // Act
        configuration.GetSection("Authentication:Schemes:Facebook").Bind(facebookOptions);

        // Assert
        facebookOptions.AppId.Should().NotBeNullOrEmpty();
        facebookOptions.AppSecret.Should().NotBeNullOrEmpty();
        facebookOptions.SaveTokens.Should().BeTrue();
    }

    [Fact]
    public void Contain_Authentication_Scheme_Microsoft()
    {
        // Arrange
        var microsoftAccountOptions = new MicrosoftAccountOptions();
        var configuration = _factory.Services.GetRequiredService<IConfiguration>();

        // Act
        configuration.GetSection("Authentication:Schemes:Microsoft").Bind(microsoftAccountOptions);

        // Assert
        microsoftAccountOptions.ClientId.Should().NotBeNullOrEmpty();
        microsoftAccountOptions.ClientSecret.Should().NotBeNullOrEmpty();
        microsoftAccountOptions.SaveTokens.Should().BeTrue();
    }

    [Fact]
    public void Contain_Authentication_Scheme_Google()
    {
        // Arrange
        var facebookOptions = new GoogleOptions();
        var configuration = _factory.Services.GetRequiredService<IConfiguration>();

        // Act
        configuration.GetSection("Authentication:Schemes:Google").Bind(facebookOptions);

        // Assert
        facebookOptions.ClientId.Should().NotBeNullOrEmpty();
        facebookOptions.ClientSecret.Should().NotBeNullOrEmpty();
        facebookOptions.SaveTokens.Should().BeTrue();
    }

    [Fact]
    public void Contain_ConnectionString_JordnaerDbContext()
    {
        // Arrange
        var configuration = _factory.Services.GetRequiredService<IConfiguration>();

        // Act
        string? connectionString = configuration.GetConnectionString(nameof(JordnaerDbContext));

        // Assert
        connectionString.Should().NotBeNullOrEmpty();
    }
}
