using Refit;

namespace Jordnaer.Shared;

/// <summary>
/// Refit Client used to ping the Data Forsyningen API
/// </summary>
/// <remarks>
///     <seealso href="https://docs.dataforsyningen.dk/#dawa-danmarks-adressers-web-api"/>
/// </remarks>
public interface IDataForsyningenPingClient
{
	/// <summary>
	/// Pings the <c>/postnumre</c> endpoint, returning the least amount of data possible.
	/// <para>
	/// Used to check the health of the Data Forsyningen API.
	/// </para>
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns></returns>
	[Get("/postnumre?side=1&per_side=1")]
	Task<IApiResponse<IEnumerable<ZipCodeSearchResponse>>>
		Ping(CancellationToken cancellationToken = default);
}