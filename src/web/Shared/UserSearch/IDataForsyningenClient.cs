using Refit;

namespace Jordnaer.Shared.UserSearch;

public interface IDataForsyningenClient
{
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
