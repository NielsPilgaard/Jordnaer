namespace Jordnaer.Shared;

/// <summary>
/// Converts ETRS89 / UTM zone 32N (EPSG:25832) coordinates to latitude/longitude.
/// ETRS89 and WGS84 differ by well under a meter in Denmark, so the result can be used as WGS84 (EPSG:4326).
/// </summary>
/// <remarks>
/// Uses the Krüger series for the inverse transverse Mercator projection on the GRS80 ellipsoid,
/// accurate to well below a millimeter within the zone.
/// </remarks>
public static class Utm32Converter
{
	private const double SemiMajorAxis = 6378137.0;
	private const double Flattening = 1 / 298.257222101;
	private const double ScaleFactor = 0.9996;
	private const double FalseEasting = 500_000.0;
	private const double CentralMeridianDegrees = 9.0;

	private static readonly double N = Flattening / (2 - Flattening);
	private static readonly double RectifyingRadius =
		SemiMajorAxis / (1 + N) * (1 + N * N / 4 + N * N * N * N / 64);

	private static readonly double[] Beta =
	[
		N / 2 - 2.0 / 3 * N * N + 37.0 / 96 * N * N * N,
		1.0 / 48 * N * N + 1.0 / 15 * N * N * N,
		17.0 / 480 * N * N * N
	];

	private static readonly double[] Delta =
	[
		2 * N - 2.0 / 3 * N * N - 2 * N * N * N,
		7.0 / 3 * N * N - 8.0 / 5 * N * N * N,
		56.0 / 15 * N * N * N
	];

	public static (double Latitude, double Longitude) ToWgs84(double easting, double northing)
	{
		var xi = northing / (ScaleFactor * RectifyingRadius);
		var eta = (easting - FalseEasting) / (ScaleFactor * RectifyingRadius);

		var xiPrime = xi;
		var etaPrime = eta;
		for (var j = 1; j <= 3; j++)
		{
			xiPrime -= Beta[j - 1] * Math.Sin(2 * j * xi) * Math.Cosh(2 * j * eta);
			etaPrime -= Beta[j - 1] * Math.Cos(2 * j * xi) * Math.Sinh(2 * j * eta);
		}

		var chi = Math.Asin(Math.Sin(xiPrime) / Math.Cosh(etaPrime));

		var latitude = chi;
		for (var j = 1; j <= 3; j++)
		{
			latitude += Delta[j - 1] * Math.Sin(2 * j * chi);
		}

		var longitude = CentralMeridianDegrees * Math.PI / 180 + Math.Atan(Math.Sinh(etaPrime) / Math.Cos(xiPrime));

		return (latitude * 180 / Math.PI, longitude * 180 / Math.PI);
	}
}
