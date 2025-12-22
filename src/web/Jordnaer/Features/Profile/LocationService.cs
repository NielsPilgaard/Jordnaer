using Jordnaer.Shared;
using NetTopologySuite.Geometries;

namespace Jordnaer.Features.Profile;

public record LocationResult(Point Location, int? ZipCode, string? City);

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
	IDataForsyningenClient dataForsyningenClient,
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

		// Get address suggestions first
		var addressResponse = await dataForsyningenClient.GetAddressesWithAutoComplete(addressText, cancellationToken);

		if (!addressResponse.IsSuccessful || addressResponse.Content is null)
		{
			logger.LogWarning("Failed to get address autocomplete for: {AddressText}", addressText);
			return null;
		}

		var firstAddress = addressResponse.Content.FirstOrDefault();
		if (firstAddress.Adresse is null)
		{
			logger.LogWarning("No address found for: {AddressText}", addressText);
			return null;
		}

		var adresse = firstAddress.Adresse.Value;

		// Extract zip code and city
		int? zipCode = null;
		if (!string.IsNullOrEmpty(adresse.Postnr) && int.TryParse(adresse.Postnr, out var parsedZipCode))
		{
			zipCode = parsedZipCode;
		}

		// In DataForsyningen, X = longitude, Y = latitude
		// NetTopologySuite Point uses (longitude, latitude) order
		var location = GeometryFactory.CreatePoint(new Coordinate(adresse.X, adresse.Y));

		return new LocationResult(location, zipCode, adresse.Postnrnavn);
	}

	public async Task<LocationResult?> GetLocationFromZipCodeAsync(
		string zipCodeText,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(zipCodeText))
		{
			return null;
		}

		// Get zip code suggestions first
		var zipCodeResponse = await dataForsyningenClient.GetZipCodesWithAutoComplete(zipCodeText, cancellationToken);

		if (!zipCodeResponse.IsSuccessful || zipCodeResponse.Content is null)
		{
			logger.LogWarning("Failed to get zip code autocomplete for: {ZipCodeText}", zipCodeText);
			return null;
		}

		var firstZipCode = zipCodeResponse.Content.FirstOrDefault();
		if (firstZipCode.Postnummer is null)
		{
			logger.LogWarning("No zip code found for: {ZipCodeText}", zipCodeText);
			return null;
		}

		var postnummer = firstZipCode.Postnummer.Value;

		// Extract zip code
		int? zipCode = null;
		if (!string.IsNullOrEmpty(postnummer.Nr) && int.TryParse(postnummer.Nr, out var parsedZipCode))
		{
			zipCode = parsedZipCode;
		}

		// In DataForsyningen, Visueltcenter_x = longitude, Visueltcenter_y = latitude
		// NetTopologySuite Point uses (longitude, latitude) order
		var location = GeometryFactory.CreatePoint(new Coordinate(postnummer.Visueltcenter_x, postnummer.Visueltcenter_y));

		return new LocationResult(location, zipCode, postnummer.Navn);
	}
}
