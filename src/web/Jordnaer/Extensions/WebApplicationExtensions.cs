using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;

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

	/// <summary>
	/// Use forwarded headers to get correct request scheme and client IP when running behind reverse proxies/load balancers.
	/// <para>
	/// This is necessary for correct OIDC redirects when using external login providers.
	/// </para>
	/// <para>
	/// See /docs/forwarded-headers-azure.md for why we clear KnownProxies and KnownNetworks.
	/// </para>
	/// </summary>
	/// <param name="app"></param>
	/// <returns></returns>
	public static WebApplication UseReverseProxyHeaderForwarding(this WebApplication app)
	{
		var forwardedHeadersOptions = new ForwardedHeadersOptions
		{
			ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
		};

		// Intentionally clear for Azure App Service - see /docs/forwarded-headers-azure.md
		forwardedHeadersOptions.KnownIPNetworks.Clear();
		forwardedHeadersOptions.KnownProxies.Clear();

		app.UseForwardedHeaders(forwardedHeadersOptions);

		return app;
	}
}