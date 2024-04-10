using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Jordnaer.Extensions;

public static class WebApplicationExtensions
{
	public static WebApplication MapObservabilityEndpoints(this WebApplication app)
	{
		// All health checks must pass for app to be considered ready to accept traffic after starting
		app.MapHealthChecks("/health").AllowAnonymous().RequireHealthCheckRateLimit();

		// Only health checks tagged with the "live" tag must pass for app to be considered alive
		app.MapHealthChecks("/alive", new HealthCheckOptions
		{
			Predicate = registration => registration.Tags.Contains("live")
		}).AllowAnonymous().RequireHealthCheckRateLimit();

		// elmah.io sends head requests to check if the server is alive
		app.MapMethods("/", [HttpMethods.Head, HttpMethods.Options], () => "Ok");

		return app;
	}
}