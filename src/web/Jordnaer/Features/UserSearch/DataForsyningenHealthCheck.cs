using Jordnaer.Shared;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Refit;

namespace Jordnaer.Features.UserSearch;

public class DataForsyningenHealthCheck(
	IDataForsyningenPingClient dataForsyningenPingClient,
	ILogger<DataForsyningenHealthCheck> logger)
	: IHealthCheck
{
	private static readonly HealthCheckResult Healthy = new(HealthStatus.Healthy);

	public async Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		IApiResponse<IEnumerable<ZipCodeSearchResponse>> pingResult;
		try
		{
			pingResult = await dataForsyningenPingClient.Ping(cancellationToken);
		}
		catch (BrokenCircuitException exception)
		{
			logger.LogDebug("Circuit Breaker has been triggered.");
			return HealthCheckResult.Degraded(exception.Message);
		}
		catch (RateLimiterRejectedException exception)
		{
			logger.LogDebug("Internal healthcheck rate limit has been reached.");
			return HealthCheckResult.Degraded(exception.Message);
		}

		if (pingResult.IsSuccessful)
		{
			return Healthy;
		}

		if (pingResult.StatusCode is HttpStatusCode.TooManyRequests)
		{
			logger.LogWarning("The {healthCheckName} has hit the rate limit.", nameof(DataForsyningenHealthCheck));
			return Healthy;
		}

		return pingResult.Error is not null
				   ? new HealthCheckResult(HealthStatus.Degraded, pingResult.Error.Message, pingResult.Error)
				   : Healthy;
	}
}
