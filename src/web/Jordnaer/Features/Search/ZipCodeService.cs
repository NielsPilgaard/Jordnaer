using Jordnaer.Shared;
using Microsoft.Extensions.Options;

namespace Jordnaer.Features.Search;

public interface IZipCodeService
{
	Task<(List<int> ZipCodes, int? SearchedZipCode)> GetZipCodesNearLocationAsync(string location, int? withinRadiusKilometers,
		CancellationToken cancellationToken = default);
}

public class ZipCodeService(IDataForsyningenClient dataForsyningenClient, IOptions<DataForsyningenOptions> config, ILogger<ZipCodeService> logger) : IZipCodeService
{
	public async Task<(List<int> ZipCodes, int? SearchedZipCode)> GetZipCodesNearLocationAsync(string location, int? withinRadiusKilometers,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrEmpty(location);

		var searchedZipCode = GetZipCodeFromLocation(location);

		logger.LogDebug("ZipCode that will be searched for is: {searchedZipCode}", searchedZipCode);

		var searchResponse = await dataForsyningenClient.GetZipCodesWithAutoComplete(
								 location, cancellationToken);
		if (!searchResponse.IsSuccessful || searchResponse.Content is null)
		{
			logger.LogError(searchResponse.Error,
							"Exception occurred in function {functionName} " +
							"while calling external API through {externalApiFunction}. " +
							"{statusCode}: {reasonPhrase}",
							nameof(GetZipCodesNearLocationAsync),
							nameof(IDataForsyningenClient.GetZipCodesWithAutoComplete),
							searchResponse.StatusCode,
							searchResponse.ReasonPhrase);
			return ([], searchedZipCode);
		}

		var zipCode = searchResponse.Content?.FirstOrDefault().Postnummer;
		if (zipCode is null)
		{
			logger.LogError("{responseName} was null.", $"{nameof(ZipCodeAutoCompleteResponse)}.{nameof(ZipCodeAutoCompleteResponse.Postnummer)}");
			return ([], searchedZipCode);
		}

		var searchRadiusMeters = Math.Min(withinRadiusKilometers ?? 0,
										  config.Value.MaxSearchRadiusKilometers) * 1000;

		var circle = Circle.FromZipCode(zipCode.Value, searchRadiusMeters);

		var zipCodeSearchResponse = await dataForsyningenClient.GetZipCodesWithinCircle(circle.ToString(), cancellationToken);
		if (!zipCodeSearchResponse.IsSuccessful)
		{
			logger.LogError("Failed to get zip codes within {radius}m of the coordinates x={x}, y={y}", withinRadiusKilometers, zipCode.Value.Visueltcenter_x, zipCode.Value.Visueltcenter_y);
			return ([], searchedZipCode);
		}

		var zipCodesWithinCircle = zipCodeSearchResponse
								   .Content!
								   .Select(zipCodeResponse => int.Parse(zipCodeResponse.Nr!))
								   .ToList();

		logger.LogDebug("Returned zip codes: {zipCodeCount}", zipCodesWithinCircle.Count);

		return (zipCodesWithinCircle, searchedZipCode);
	}

	/// <summary>
	/// The location string is in the format '8550 Ryomgård', so the first 4 characters are the zip code.
	/// </summary>
	/// <param name="location"></param>
	/// <returns></returns>
	private static int GetZipCodeFromLocation(string location)
	{
		var zipCodeSpan = location.AsSpan()[..4];

		return int.Parse(zipCodeSpan);
	}
}