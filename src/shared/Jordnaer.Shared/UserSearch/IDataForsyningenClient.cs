using Refit;

namespace Jordnaer.Shared;

/// <summary>
/// Refit Client used to interact with the Data Forsyningen API
/// </summary>
/// <remarks>
///     <seealso href="https://dawadocs.dataforsyningen.dk/dok/api"/>
/// </remarks>
public interface IDataForsyningenClient
{
	/// <summary>
	/// Gets the addresses that match the <paramref name="query"/>, with autocomplete.
	/// </summary>
	/// <param name="query">The query.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns></returns>
	[Get("/adresser/autocomplete")]
	Task<IApiResponse<IEnumerable<AddressAutoCompleteResponse>>>
		GetAddressesWithAutoComplete([AliasAs("q")] string? query, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the zip codes that match the <paramref name="query"/>, with autocomplete.
	/// </summary>
	/// <param name="query">The query.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns></returns>
	[Get("/postnumre/autocomplete")]
	Task<IApiResponse<IEnumerable<ZipCodeAutoCompleteResponse>>>
		GetZipCodesWithAutoComplete([AliasAs("q")] string? query, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the zip code that matches the coordinates.
	/// </summary>
	/// <param name="longitude"></param>
	/// <param name="latitude"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	[Get("/postnumre/reverse")]
	Task<IApiResponse<ZipCodeSearchResponse>>
		GetZipCodeFromCoordinates(
			[AliasAs("x")] string longitude,
			[AliasAs("y")] string latitude,
			CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the zip codes within the circle.
	/// </summary>
	/// <param name="circle">The circle, in the format {x,y,radius}, where
	/// x and y are coordinates, and radius is meters from the coordinates</param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	[Get("/postnumre")]
	[QueryUriFormat(UriFormat.Unescaped)]
	Task<IApiResponse<IEnumerable<ZipCodeSearchResponse>>>
		GetZipCodesWithinCircle([AliasAs("cirkel")] string? circle, CancellationToken cancellationToken = default);
}