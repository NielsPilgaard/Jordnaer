using System.Net;
using Jordnaer.Shared.UserSearch;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Jordnaer.Server.Features.UserSearch;

public class DataForsyningenHealthCheck : IHealthCheck
{
    private readonly IDataForsyningenClient _dataForsyningenClient;
    private readonly ILogger<DataForsyningenHealthCheck> _logger;

    public DataForsyningenHealthCheck(IDataForsyningenClient dataForsyningenClient, ILogger<DataForsyningenHealthCheck> logger)
    {
        _dataForsyningenClient = dataForsyningenClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var pingResult = await _dataForsyningenClient.Ping(cancellationToken);

        if (pingResult.IsSuccessStatusCode)
        {
            return HealthCheckResult.Healthy();
        }
        if (pingResult.StatusCode is HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning("The {healthCheckName} has hit the rate limit.", nameof(DataForsyningenHealthCheck));
            return HealthCheckResult.Healthy();
        }
        if (pingResult.Error is not null)
        {
            return new HealthCheckResult(HealthStatus.Degraded, pingResult.Error.Message, pingResult.Error);
        }

        return HealthCheckResult.Healthy();
    }
}
