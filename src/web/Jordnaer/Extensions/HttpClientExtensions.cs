using Polly;
using Polly.Contrib.WaitAndRetry;

namespace Jordnaer.Extensions;

public static class HttpClientExtensions
{
	public static IHttpClientBuilder AddResilientHttpClient(this IServiceCollection services) =>
		services.AddHttpClient(HttpClients.External)
			// TODO: Use the new Microsoft.Extensions.Resilience methods
			.AddTransientHttpErrorPolicy(policyBuilder =>
				policyBuilder.WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(250), 3)));
}

public static class HttpClients
{
	public const string External = "external";
}
