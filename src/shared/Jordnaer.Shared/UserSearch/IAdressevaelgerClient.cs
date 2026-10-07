using Refit;

namespace Jordnaer.Shared;

/// <summary>
/// Refit Client used to interact with Klimadatastyrelsen's Adressevælger API, the replacement for DAWA autocomplete.
/// The required <c>token</c> query parameter is appended by <see cref="AdressevaelgerTokenHandler"/>.
/// </summary>
/// <remarks>
///     <seealso href="https://confluence.kds.dk/pages/viewpage.action?pageId=234782998"/>
/// </remarks>
public interface IAdressevaelgerClient
{
	/// <summary>
	/// Phonetic search for addresses. Depending on how specific the <paramref name="query"/> is,
	/// results can be of type <c>adresse</c>, <c>husnummer</c>, <c>navngivenvejpostnummer</c> or <c>vejnavn</c>.
	/// Results contain no coordinates; use <see cref="GetHusnummerAsync"/> for those.
	/// </summary>
	/// <param name="query">Free text, e.g. "Park Allé 1, 8000 Aarhus C".</param>
	/// <param name="maximum">Maximum number of results (default 100, max 200).</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	[Get("/adresser/soeg")]
	Task<IApiResponse<AdressevaelgerSearchResponse>> SearchAddressesAsync(
		[AliasAs("tekst")] string query,
		[AliasAs("maksimum")] int? maximum = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Looks up a house number (adgangsadresse) by its DAR id, including the access point coordinates.
	/// </summary>
	/// <param name="id">The <c>DAR_Husnummer.id_lokalId</c>.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	[Get("/husnumre/{id}")]
	Task<IApiResponse<AdressevaelgerHusnummerResponse>> GetHusnummerAsync(
		string id,
		CancellationToken cancellationToken = default);
}
