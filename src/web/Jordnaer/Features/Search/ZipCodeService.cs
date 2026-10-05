using System.Text.Json;

namespace Jordnaer.Features.Search;

/// <summary>
/// A Danish zip code with its visual center and bounding box (WGS84).
/// </summary>
/// <param name="BoundingBox">[minLongitude, minLatitude, maxLongitude, maxLatitude]</param>
public sealed record DanishZipCode(int Number, string Name, double Latitude, double Longitude, double[] BoundingBox)
{
	public bool BoundingBoxContains(double latitude, double longitude) =>
		longitude >= BoundingBox[0] && latitude >= BoundingBox[1] &&
		longitude <= BoundingBox[2] && latitude <= BoundingBox[3];

	/// <summary>
	/// Format used throughout the app, e.g. "8550 Ryomgård".
	/// </summary>
	public override string ToString() => $"{Number:D4} {Name}";
}

public interface IZipCodeService
{
	/// <summary>
	/// Autocomplete search on zip code number and city name, best matches first.
	/// </summary>
	IReadOnlyList<DanishZipCode> Search(string? query, int maxResults = 20);

	/// <summary>
	/// Finds the zip code from text like "8550 Ryomgård", "8550" or "Ryomgård".
	/// </summary>
	DanishZipCode? Find(string? zipCodeOrCity);

	/// <summary>
	/// Finds the zip code area that best contains the coordinates, or <c>null</c> if they are outside Denmark.
	/// </summary>
	DanishZipCode? FindNearest(double latitude, double longitude);
}

/// <summary>
/// Looks up Danish zip codes from an embedded dataset, so no external API is needed.
/// The dataset is a snapshot of DAWA's <c>/postnumre</c> endpoint taken shortly before DAWA shut down (October 2026).
/// Zip codes rarely change, but the snapshot should be refreshed if new ones are introduced.
/// </summary>
public class ZipCodeService : IZipCodeService
{
	private const string ResourceName = "Jordnaer.Features.Search.Data.postnumre.json";

	/// <summary>
	/// Coordinates further than this from any zip code center, and outside all bounding boxes, are considered outside Denmark.
	/// </summary>
	private const double MaxNearestDistanceKilometers = 25;

	private static readonly Lazy<IReadOnlyList<DanishZipCode>> ZipCodes = new(LoadZipCodes);
	private static readonly Lazy<Dictionary<int, DanishZipCode>> ZipCodesByNumber =
		new(() => ZipCodes.Value.ToDictionary(zipCode => zipCode.Number));

	public IReadOnlyList<DanishZipCode> Search(string? query, int maxResults = 20)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return [];
		}

		var trimmedQuery = query.Trim();

		return ZipCodes.Value
			.Select(zipCode => (ZipCode: zipCode, Rank: GetMatchRank(zipCode, trimmedQuery)))
			.Where(match => match.Rank is not null)
			.OrderBy(match => match.Rank)
			.ThenBy(match => match.ZipCode.Number)
			.Take(maxResults)
			.Select(match => match.ZipCode)
			.ToList();
	}

	public DanishZipCode? Find(string? zipCodeOrCity)
	{
		if (string.IsNullOrWhiteSpace(zipCodeOrCity))
		{
			return null;
		}

		var text = zipCodeOrCity.Trim();

		if (text.Length >= 4 && text[..4].All(char.IsAsciiDigit))
		{
			return ZipCodesByNumber.Value.GetValueOrDefault(int.Parse(text[..4]));
		}

		return ZipCodes.Value.FirstOrDefault(zipCode => zipCode.Name.Equals(text, StringComparison.OrdinalIgnoreCase))
			   ?? Search(text, maxResults: 1).FirstOrDefault();
	}

	public DanishZipCode? FindNearest(double latitude, double longitude)
	{
		var containing = ZipCodes.Value
			.Where(zipCode => zipCode.BoundingBoxContains(latitude, longitude))
			.MinBy(zipCode => DistanceKilometers(zipCode, latitude, longitude));

		if (containing is not null)
		{
			return containing;
		}

		var nearest = ZipCodes.Value.MinBy(zipCode => DistanceKilometers(zipCode, latitude, longitude));

		return nearest is not null && DistanceKilometers(nearest, latitude, longitude) <= MaxNearestDistanceKilometers
			? nearest
			: null;
	}

	/// <summary>
	/// Lower is better, <c>null</c> means no match.
	/// </summary>
	private static int? GetMatchRank(DanishZipCode zipCode, string query)
	{
		var number = zipCode.Number.ToString("D4");
		if (number == query)
		{
			return 0;
		}

		if (zipCode.ToString().StartsWith(query, StringComparison.OrdinalIgnoreCase))
		{
			return 1;
		}

		if (zipCode.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
		{
			return 2;
		}

		if (zipCode.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
		{
			return 3;
		}

		return null;
	}

	/// <summary>
	/// Equirectangular approximation, which is plenty accurate at the distances within Denmark.
	/// </summary>
	private static double DistanceKilometers(DanishZipCode zipCode, double latitude, double longitude)
	{
		const double earthRadiusKilometers = 6371;
		var meanLatitude = (zipCode.Latitude + latitude) / 2 * Math.PI / 180;
		var x = (longitude - zipCode.Longitude) * Math.PI / 180 * Math.Cos(meanLatitude);
		var y = (latitude - zipCode.Latitude) * Math.PI / 180;

		return Math.Sqrt(x * x + y * y) * earthRadiusKilometers;
	}

	private static IReadOnlyList<DanishZipCode> LoadZipCodes()
	{
		using var stream = typeof(ZipCodeService).Assembly.GetManifestResourceStream(ResourceName)
						   ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' was not found.");

		return JsonSerializer.Deserialize<List<DanishZipCode>>(stream, JsonSerializerOptions.Web)
			   ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' could not be deserialized.");
	}
}
