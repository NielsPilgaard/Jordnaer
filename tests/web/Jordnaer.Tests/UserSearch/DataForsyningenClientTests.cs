using FluentAssertions;
using Jordnaer.Shared;
using Xunit;

namespace Jordnaer.Tests.UserSearch;

[Trait("Category", "IntegrationTest")]
public class DataForsyningenClientTests
{
	private readonly IDataForsyningenClient _dataForsyningenClient = Refit.RestService.For<IDataForsyningenClient>(
		new HttpClient { BaseAddress = new Uri("https://api.dataforsyningen.dk") });

	private readonly IDataForsyningenPingClient _dataForsyningenPingClient = Refit.RestService.For<IDataForsyningenPingClient>(
		new HttpClient { BaseAddress = new Uri("https://api.dataforsyningen.dk") });

	[Fact]
	public async Task Get_Addresses_With_AutoComplete()
	{
		// Arrange
		const string query = "8000"; // ZipCode of Aarhus C

		// Act
		var response = await _dataForsyningenClient.GetAddressesWithAutoComplete(query);

		// Assert
		response.IsSuccessStatusCode.Should().BeTrue();
		response.Content.Should().NotBeNull().And.HaveCountGreaterThan(0);
	}

	[Fact]
	public async Task Get_ZipCodes_With_AutoComplete()
	{
		// Arrange
		const string query = "8000"; // ZipCode of Aarhus C

		// Act
		var response = await _dataForsyningenClient.GetZipCodesWithAutoComplete(query);

		// Assert
		response.IsSuccessStatusCode.Should().BeTrue();
		response.Content.Should().NotBeNull().And.HaveCountGreaterThan(0);
	}

	[Fact]
	public async Task Get_ZipCodes_Within_Circle()
	{
		// Arrange
		const string circle = "10.19268872,56.14265979,1000"; // Coordinates for Aarhus C

		// Act
		var response = await _dataForsyningenClient.GetZipCodesWithinCircle(circle);

		// Assert
		response.IsSuccessStatusCode.Should().BeTrue();
		response.Content.Should().NotBeNull().And.HaveCountGreaterThan(0);
	}

	[Fact]
	public async Task Ping_Data_Forsyningen_API()
	{
		// Act
		var response = await _dataForsyningenPingClient.Ping();

		// Assert
		response.IsSuccessStatusCode.Should().BeTrue();
		response.Content.Should().NotBeNull().And.HaveCount(1);
	}
}
