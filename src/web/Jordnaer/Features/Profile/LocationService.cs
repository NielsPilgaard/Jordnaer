using Jordnaer.Features.Search;
using Jordnaer.Shared;
using NetTopologySuite.Geometries;

namespace Jordnaer.Features.Profile;

public record LocationResult(Point Location, int? ZipCode, string? City, Point? ZipCodeLocation = null);

public interface ILocationService
{
	/// <summary>
	/// Extracts coordinates from an address autocomplete response text.
	/// </summary>
	/// <param name="addressText">The address text from autocomplete (format: "Street, Zip City")</param>
	/// <param name="cancellationToken"></param>
	/// <returns>Location result or null if not found</returns>
	Task<LocationResult?> GetLocationFromAddressAsync(
		string addressText,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Extracts coordinates from a zip code text.
	/// </summary>
	/// <param name="zipCodeText">The zip code text (format: "8550 Ryomgård")</param>
	/// <param name="cancellationToken"></param>
	/// <returns>Location result or null if not found</returns>
	Task<LocationResult?> GetLocationFromZipCodeAsync(
		string zipCodeText,
		CancellationToken cancellationToken = default);
}

public class LocationService(
	IAdressevaelgerClient adressevaelgerClient,
	IZipCodeService zipCodeService,
	ILogger<LocationService> logger) : ILocationService
{
	private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);

	public async Task<LocationResult?> GetLocationFromAddressAsync(
		string addressText,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(addressText))
		{
			return null;
		}

		// The address text usually comes from autocomplete, so the best match is the address itself
		var searchResponse = await adressevaelgerClient.SearchAddressesAsync(addressText, maximum: 1, cancellationToken);

		if (!searchResponse.IsSuccessful || searchResponse.Content is null)
		{
			logger.LogWarning(searchResponse.Error,
				"Failed to search for address: {AddressText}. StatusCode: {StatusCode}",
				addressText, searchResponse.StatusCode);
			return null;
		}

		var firstMatch = searchResponse.Content.Fund?.FirstOrDefault();
		var husnummerId = firstMatch?.GetHusnummerId();
		if (husnummerId is null)
		{
			// Only a street was matched (e.g. "Ryomgård Midtpunkt 8550 Ryomgård"), fall back to its zip code
			if (firstMatch?.Type is AdressevaelgerFund.NavngivenVejPostnummerType &&
				await GetLocationFromZipCodeAsync(firstMatch.Postnr ?? string.Empty, cancellationToken) is { } streetZipCodeResult)
			{
				return streetZipCodeResult with { ZipCodeLocation = streetZipCodeResult.Location };
			}

			logger.LogWarning("No address found for: {AddressText}", addressText);
			return null;
		}

		// Search results contain no coordinates, so we need to look up the house number
		var husnummerResponse = await adressevaelgerClient.GetHusnummerAsync(husnummerId, cancellationToken);
		var husnummer = husnummerResponse.Content?.Husnummer;
		var coordinates = husnummer?.Adgangspunkt?.Koordinater;
		if (!husnummerResponse.IsSuccessful || coordinates is null)
		{
			logger.LogWarning(husnummerResponse.Error,
				"Failed to get coordinates for house number {HusnummerId} ({AddressText}). StatusCode: {StatusCode}",
				husnummerId, addressText, husnummerResponse.StatusCode);
			return null;
		}

		// Adressevælger returns ETRS89 / UTM32 coordinates, while we store WGS84
		// NetTopologySuite Point uses (longitude, latitude) order
		var (latitude, longitude) = coordinates.ToWgs84();
		var location = GeometryFactory.CreatePoint(new Coordinate(longitude, latitude));

		var postnummer = husnummer!.Postnummer;
		int? zipCode = int.TryParse(postnummer?.Postnr, out var parsedZipCode) ? parsedZipCode : null;

		// Also look up the zip code center coordinates for privacy (non-members see this instead of exact address)
		var zipCodeLocation = zipCode is not null
			? (await GetLocationFromZipCodeAsync(postnummer!.Postnr!, cancellationToken))?.Location
			: null;

		return new LocationResult(location, zipCode, postnummer?.Navn, zipCodeLocation);
	}

	public Task<LocationResult?> GetLocationFromZipCodeAsync(
		string zipCodeText,
		CancellationToken cancellationToken = default)
	{
		var zipCode = zipCodeService.Find(zipCodeText);
		if (zipCode is null)
		{
			if (!string.IsNullOrWhiteSpace(zipCodeText))
			{
				logger.LogWarning("No zip code found for: {ZipCodeText}", zipCodeText);
			}

			return Task.FromResult<LocationResult?>(null);
		}

		// NetTopologySuite Point uses (longitude, latitude) order
		var location = GeometryFactory.CreatePoint(new Coordinate(zipCode.Longitude, zipCode.Latitude));

		return Task.FromResult<LocationResult?>(new LocationResult(location, zipCode.Number, zipCode.Name));
	}
}
