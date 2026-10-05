using FluentAssertions;
using Jordnaer.Features.Profile;
using Jordnaer.Features.Search;
using Jordnaer.Shared;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Refit;
using System.Net;
using Xunit;

namespace Jordnaer.Tests.Profile;

public sealed class LocationServiceTests : IDisposable
{
	private const string HusnummerId = "0a3f507b-24ba-32b8-e044-0003ba298018";

	private readonly IAdressevaelgerClient _client = Substitute.For<IAdressevaelgerClient>();
	private readonly List<IDisposable> _disposables = [];

	private LocationService CreateSut() =>
		new(_client, new ZipCodeService(), Substitute.For<ILogger<LocationService>>());

	[Fact]
	public async Task GetLocationFromAddressAsync_ReturnsWgs84Location_AndZipCodeCenter()
	{
		SetupSearch(new AdressevaelgerFund("adresse", "some-address-id", "Vestergade 12, 1456 København K",
			null, null, null, null, HusnummerId));
		SetupHusnummer(new AdressevaelgerHusnummer(HusnummerId, "12", "Vestergade 12, 1456 København K", "Vestergade",
			new AdressevaelgerAdgangspunkt(new AdressevaelgerKoordinater(724468.63, 6176001.31)),
			new AdressevaelgerPostnummer("København K", "1456")));

		var result = await CreateSut().GetLocationFromAddressAsync("Vestergade 12, 1456 København K");

		result.Should().NotBeNull();
		result!.ZipCode.Should().Be(1456);
		result.City.Should().Be("København K");
		result.Location.Y.Should().BeApproximately(55.6778, 0.001);
		result.Location.X.Should().BeApproximately(12.5703, 0.001);
		result.ZipCodeLocation.Should().NotBeNull();
		result.ZipCodeLocation!.Y.Should().BeApproximately(55.677, 0.01);
		await _client.Received(1).GetHusnummerAsync(HusnummerId, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetLocationFromAddressAsync_UsesIdDirectly_ForHusnummerResult()
	{
		SetupSearch(new AdressevaelgerFund("husnummer", HusnummerId, "Vestergade 12, 1456 København K",
			"Vestergade", "12", null, null, null));
		SetupHusnummer(new AdressevaelgerHusnummer(HusnummerId, "12", "Vestergade 12, 1456 København K", "Vestergade",
			new AdressevaelgerAdgangspunkt(new AdressevaelgerKoordinater(724468.63, 6176001.31)),
			new AdressevaelgerPostnummer("København K", "1456")));

		var result = await CreateSut().GetLocationFromAddressAsync("Vestergade 12, 1456 København K");

		result.Should().NotBeNull();
		await _client.Received(1).GetHusnummerAsync(HusnummerId, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetLocationFromAddressAsync_FallsBackToZipCode_WhenOnlyStreetMatches()
	{
		SetupSearch(new AdressevaelgerFund("navngivenvejpostnummer", "street-id", "Ryomgård Midtpunkt 8550 Ryomgård",
			"Ryomgård Midtpunkt", null, "8550", "Ryomgård", null));

		var result = await CreateSut().GetLocationFromAddressAsync("Ryomgård Midtpunkt");

		result.Should().NotBeNull();
		result!.ZipCode.Should().Be(8550);
		result.ZipCodeLocation.Should().Be(result.Location);
		await _client.DidNotReceive().GetHusnummerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetLocationFromAddressAsync_ReturnsNull_WhenNothingFound()
	{
		SetupSearch();

		var result = await CreateSut().GetLocationFromAddressAsync("nowhere");

		result.Should().BeNull();
	}

	[Fact]
	public async Task GetLocationFromAddressAsync_ReturnsNull_WhenSearchFails()
	{
		var httpResponse = Track(new HttpResponseMessage(HttpStatusCode.GatewayTimeout));
		var apiResponse = Track(new ApiResponse<AdressevaelgerSearchResponse>(httpResponse, null, new RefitSettings()));
		_client.SearchAddressesAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
			.Returns(apiResponse);

		var result = await CreateSut().GetLocationFromAddressAsync("Vestergade 12, 1456 København K");

		result.Should().BeNull();
	}

	[Fact]
	public async Task GetLocationFromZipCodeAsync_ReturnsZipCodeCenter()
	{
		var result = await CreateSut().GetLocationFromZipCodeAsync("8550 Ryomgård");

		result.Should().NotBeNull();
		result!.ZipCode.Should().Be(8550);
		result.City.Should().Be("Ryomgård");
		result.Location.Y.Should().BeInRange(56, 57);
		result.Location.X.Should().BeInRange(10, 11);
		await _client.DidNotReceiveWithAnyArgs().SearchAddressesAsync(default!);
	}

	[Theory]
	[InlineData("")]
	[InlineData("0001 Nowhere")]
	public async Task GetLocationFromZipCodeAsync_ReturnsNull_ForUnknownZipCode(string text)
	{
		var result = await CreateSut().GetLocationFromZipCodeAsync(text);

		result.Should().BeNull();
	}

	private void SetupSearch(params AdressevaelgerFund[] results)
	{
		var httpResponse = Track(new HttpResponseMessage(HttpStatusCode.OK));
		var apiResponse = Track(new ApiResponse<AdressevaelgerSearchResponse>(
			httpResponse, new AdressevaelgerSearchResponse("ok", "", [.. results]), new RefitSettings()));
		_client.SearchAddressesAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
			.Returns(apiResponse);
	}

	private void SetupHusnummer(AdressevaelgerHusnummer husnummer)
	{
		var httpResponse = Track(new HttpResponseMessage(HttpStatusCode.OK));
		var apiResponse = Track(new ApiResponse<AdressevaelgerHusnummerResponse>(
			httpResponse, new AdressevaelgerHusnummerResponse("ok", husnummer), new RefitSettings()));
		_client.GetHusnummerAsync(husnummer.Id_lokalid!, Arg.Any<CancellationToken>())
			.Returns(apiResponse);
	}

	private T Track<T>(T disposable) where T : IDisposable
	{
		_disposables.Add(disposable);
		return disposable;
	}

	public void Dispose()
	{
		foreach (var disposable in _disposables)
		{
			disposable.Dispose();
		}
	}
}
