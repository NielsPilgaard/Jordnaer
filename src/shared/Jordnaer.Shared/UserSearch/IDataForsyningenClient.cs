using Refit;

namespace Jordnaer.Shared;

/// <summary>
/// Refit Client used to interact with the Data Forsyningen API
/// </summary>
/// <remarks>
///     <seealso href="https://docs.dataforsyningen.dk/#dawa-danmarks-adressers-web-api"/>
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
	public Task<IApiResponse<IEnumerable<AddressAutoCompleteResponse>>>
		GetAddressesWithAutoComplete([AliasAs("q")] string? query, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the zip codes within the circle.
	/// </summary>
	/// <param name="circle">The circle, in the format {x,y,radius}, where
	/// x and y are coordinates, and radius is meters from the coordinates</param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	[Get("/postnumre")]
	[QueryUriFormat(UriFormat.Unescaped)]
	public Task<IApiResponse<IEnumerable<ZipCodeSearchResponse>>>
		GetZipCodesWithinCircle([AliasAs("cirkel")] string? circle, CancellationToken cancellationToken = default);
}