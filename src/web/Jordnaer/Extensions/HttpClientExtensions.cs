using Polly;

namespace Jordnaer.Extensions;

public static class HttpClientExtensions
{
	public static IHttpClientBuilder AddResilientHttpClient(this IServiceCollection services) =>
		services.AddHttpClient(HttpClients.External)
			.AddTransientHttpErrorPolicy(policyBuilder =>
				policyBuilder.WaitAndRetryAsync(3, retryCount => TimeSpan.FromMilliseconds(50 * retryCount)));
}

public static class HttpClients
{
	public const string External = "external";
}
