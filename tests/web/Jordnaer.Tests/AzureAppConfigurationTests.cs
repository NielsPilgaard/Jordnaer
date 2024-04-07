using FluentAssertions;
using Jordnaer.Database;
using Jordnaer.Tests.Infrastructure;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Jordnaer.Tests;

[Trait("Category", "IntegrationTest")]
[Collection(nameof(JordnaerWebApplicationFactoryCollection))]
public class AzureAppConfigurationTests(JordnaerWebApplicationFactory factory)
{
	[Fact]
	public void Contain_Authentication_Scheme_Facebook()
	{
		// Arrange
		var facebookOptions = new FacebookOptions();
		var configuration = factory.Services.GetRequiredService<IConfiguration>();

		// Act
		configuration.GetSection("Authentication:Schemes:Facebook").Bind(facebookOptions);

		// Assert
		facebookOptions.AppId.Should().NotBeNullOrEmpty();
		facebookOptions.AppSecret.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public void Contain_Authentication_Scheme_Microsoft()
	{
		// Arrange
		var microsoftAccountOptions = new MicrosoftAccountOptions();
		var configuration = factory.Services.GetRequiredService<IConfiguration>();

		// Act
		configuration.GetSection("Authentication:Schemes:Microsoft").Bind(microsoftAccountOptions);

		// Assert
		microsoftAccountOptions.ClientId.Should().NotBeNullOrEmpty();
		microsoftAccountOptions.ClientSecret.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public void Contain_Authentication_Scheme_Google()
	{
		// Arrange
		var facebookOptions = new GoogleOptions();
		var configuration = factory.Services.GetRequiredService<IConfiguration>();

		// Act
		configuration.GetSection("Authentication:Schemes:Google").Bind(facebookOptions);

		// Assert
		facebookOptions.ClientId.Should().NotBeNullOrEmpty();
		facebookOptions.ClientSecret.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public void Contain_ConnectionString_JordnaerDbContext()
	{
		// Arrange
		var configuration = factory.Services.GetRequiredService<IConfiguration>();

		// Act
		var connectionString = configuration.GetConnectionString(nameof(JordnaerDbContext));

		// Assert
		connectionString.Should().NotBeNullOrEmpty();
	}
}
