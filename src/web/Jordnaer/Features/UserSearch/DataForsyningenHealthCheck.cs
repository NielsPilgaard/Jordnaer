using Jordnaer.Shared;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;

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
		var pingResult = await dataForsyningenPingClient.Ping(cancellationToken);

		if (pingResult.IsSuccessStatusCode)
		{
			return Healthy;
		}

		if (pingResult.StatusCode is HttpStatusCode.TooManyRequests)
		{
			logger.LogWarning("The {healthCheckName} has hit the rate limit.", nameof(DataForsyningenHealthCheck));
			return Healthy;
		}

		if (pingResult.Error is not null)
		{
			return new HealthCheckResult(HealthStatus.Degraded, pingResult.Error.Message, pingResult.Error);
		}

		return Healthy;
	}
}
