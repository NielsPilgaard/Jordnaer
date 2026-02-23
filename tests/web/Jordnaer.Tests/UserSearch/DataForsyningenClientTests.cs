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
		response.IsSuccessful.Should().BeTrue();
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
		response.IsSuccessful.Should().BeTrue();
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
		response.IsSuccessful.Should().BeTrue();
		response.Content.Should().NotBeNull().And.HaveCountGreaterThan(0);
	}

	[Fact]
	public async Task Ping_Data_Forsyningen_API()
	{
		// Act
		var response = await _dataForsyningenPingClient.Ping();

		// Assert
		response.IsSuccessful.Should().BeTrue();
		response.Content.Should().NotBeNull().And.HaveCount(1);
	}

	[Fact]
	public async Task SearchZipCodesAsync_ReturnsResults_ForKnownCity()
	{
		// Arrange
		const string query = "Randers";

		// Act
		var response = await _dataForsyningenClient.SearchZipCodesAsync(query);

		// Assert
		response.IsSuccessful.Should().BeTrue();
		response.Content.Should().NotBeNull().And.HaveCountGreaterThan(0);

		var first = response.Content!.First();
		first.Navn.Should().NotBeNullOrWhiteSpace();
		first.Nr.Should().NotBeNullOrWhiteSpace();
		first.Visueltcenter.Should().NotBeNull().And.HaveCountGreaterOrEqualTo(2,
			"visueltcenter must contain [longitude, latitude]");
	}

	[Fact]
	public async Task SearchZipCodesAsync_ReturnsEmpty_ForGibberishQuery()
	{
		// Arrange
		const string query = "xyzxyzxyz_nonexistent_city_999";

		// Act
		var response = await _dataForsyningenClient.SearchZipCodesAsync(query);

		// Assert
		response.IsSuccessful.Should().BeTrue();
		response.Content.Should().NotBeNull().And.BeEmpty();
	}

	[Fact]
	public async Task SearchZipCodesAsync_VisueltcenterIsLngLatOrder()
	{
		// Arrange - Aarhus C is at approx lng 10.2, lat 56.15
		const string query = "Aarhus C";

		// Act
		var response = await _dataForsyningenClient.SearchZipCodesAsync(query);

		// Assert
		response.IsSuccessful.Should().BeTrue();
		response.Content.Should().NotBeNull();
		response.Content.Should().HaveCountGreaterThan(0);
		var first = response.Content!.First();
		first.Visueltcenter.Should().HaveCountGreaterOrEqualTo(2);

		var longitude = (double)first.Visueltcenter![0];
		var latitude = (double)first.Visueltcenter[1];

		// Denmark: lng ≈ 8–15, lat ≈ 54–58
		longitude.Should().BeInRange(8, 15, "longitude should be in Danish range");
		latitude.Should().BeInRange(54, 58, "latitude should be in Danish range");
	}
}
