// ReSharper disable All
namespace Jordnaer.Shared;

public record AdressevaelgerSearchResponse(string? Status, string? Beskrivelse, List<AdressevaelgerFund>? Fund);

/// <summary>
/// A single search hit. Which fields are populated depends on <see cref="Type"/>.
/// </summary>
public record AdressevaelgerFund(
	string? Type,
	string? Id,
	string? Titel,
	string? Vejnavn,
	string? Husnummer,
	string? Postnr,
	string? Postdistrikt,
	string? HusnummerId)
{
	public const string AdresseType = "adresse";
	public const string HusnummerType = "husnummer";
	public const string NavngivenVejPostnummerType = "navngivenvejpostnummer";

	/// <summary>
	/// The id of the house number (adgangsadresse) this hit belongs to, if any.
	/// </summary>
	public string? GetHusnummerId() => Type switch
	{
		AdresseType => HusnummerId,
		HusnummerType => Id,
		_ => null
	};

	public override string ToString() => Titel ?? string.Empty;
}

public record AdressevaelgerHusnummerResponse(string? Status, AdressevaelgerHusnummer? Husnummer);

public record AdressevaelgerHusnummer(
	string? Id_lokalid,
	string? Husnummertekst,
	string? Adgangsadressebetegnelse,
	string? Vejnavn,
	AdressevaelgerAdgangspunkt? Adgangspunkt,
	AdressevaelgerPostnummer? Postnummer);

public record AdressevaelgerAdgangspunkt(AdressevaelgerKoordinater? Koordinater);

/// <summary>
/// Coordinates in ETRS89 / UTM zone 32N (EPSG:25832), where X is easting and Y is northing.
/// </summary>
public record AdressevaelgerKoordinater(double X, double Y)
{
	public (double Latitude, double Longitude) ToWgs84() => Utm32Converter.ToWgs84(X, Y);
}

public record AdressevaelgerPostnummer(string? Navn, string? Postnr);
