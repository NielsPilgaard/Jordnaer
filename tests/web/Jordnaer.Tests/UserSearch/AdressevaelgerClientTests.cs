using FluentAssertions;
using Jordnaer.Shared;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jordnaer.Tests.UserSearch;

/// <summary>
/// Calls the live Adressevælger API to catch breaking changes in its contract.
/// </summary>
[Trait("Category", "IntegrationTest")]
public class AdressevaelgerClientTests
{
	private readonly IAdressevaelgerClient _client = Refit.RestService.For<IAdressevaelgerClient>(
		new HttpClient(new AdressevaelgerTokenHandler(Options.Create(new AdressevaelgerOptions
		{
			BaseUrl = "https://adressevaelger.dk",
			Token = "adressevaelger123"
		}))
		{
			InnerHandler = new HttpClientHandler()
		})
		{
			BaseAddress = new Uri("https://adressevaelger.dk")
		});

	[Fact]
	public async Task SearchAddressesAsync_ReturnsAddress_ForFullAddress()
	{
		var response = await _client.SearchAddressesAsync("Park Allé 1, 8000 Aarhus C", maximum: 5);

		response.IsSuccessful.Should().BeTrue();
		var first = response.Content!.Fund.Should().NotBeNullOrEmpty().And.Subject.First();
		first.Titel.Should().Be("Park Allé 1, 8000 Aarhus C");
		first.Type.Should().Be(AdressevaelgerFund.AdresseType);
		first.GetHusnummerId().Should().NotBeNullOrEmpty();
	}

	[Fact]
	public async Task SearchAddressesAsync_RespectsMaximum()
	{
		var response = await _client.SearchAddressesAsync("Park Allé 1, 8000", maximum: 2);

		response.IsSuccessful.Should().BeTrue();
		response.Content!.Fund.Should().HaveCountLessThanOrEqualTo(2);
	}

	[Fact]
	public async Task GetHusnummerAsync_ReturnsCoordinatesInDenmark()
	{
		var searchResponse = await _client.SearchAddressesAsync("Park Allé 1, 8000 Aarhus C", maximum: 1);
		var husnummerId = searchResponse.Content!.Fund!.First().GetHusnummerId()!;

		var response = await _client.GetHusnummerAsync(husnummerId);

		response.IsSuccessful.Should().BeTrue();
		var husnummer = response.Content!.Husnummer!;
		husnummer.Postnummer!.Postnr.Should().Be("8000");
		husnummer.Postnummer.Navn.Should().Be("Aarhus C");

		var (latitude, longitude) = husnummer.Adgangspunkt!.Koordinater!.ToWgs84();
		latitude.Should().BeApproximately(56.15, 0.05);
		longitude.Should().BeApproximately(10.21, 0.05);
	}
}
