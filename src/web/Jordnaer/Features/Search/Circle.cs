using System.Globalization;
using Jordnaer.Shared;

namespace Jordnaer.Features.Search;

public readonly record struct Circle(float X, float Y, int RadiusMeters)
{
	private static readonly NumberFormatInfo FloatNumberFormat = new() { CurrencyDecimalSeparator = "." };

	/// <summary>
	/// Required querystring format is "cirkel=11.111,11.111,10000", or "cirkel=x,y,radius"
	/// </summary>
	/// <returns></returns>
	public override string ToString() => $"{X.ToString(FloatNumberFormat)}," +
										 $"{Y.ToString(FloatNumberFormat)}," +
										 $"{RadiusMeters}";

	public static Circle FromAddress(Adresse address, int radiusMeters)
		=> new(address.X, address.Y, radiusMeters);

	public static Circle FromZipCode(Postnummer postnummer, int radiusMeters)
		=> new(postnummer.Visueltcenter_x, postnummer.Visueltcenter_y, radiusMeters);
}