using Polly;

namespace Jordnaer.Server.Extensions;

public static class HttpClientExtensions
{
    public static IHttpClientBuilder AddResilientHttpClient(this IServiceCollection services) =>
        services.AddHttpClient(HttpClients.EXTERNAL)
                .AddTransientHttpErrorPolicy(policyBuilder =>
                    policyBuilder.WaitAndRetryAsync(3, retryCount => TimeSpan.FromMilliseconds(50 * retryCount)))
                .AddTransientHttpErrorPolicy(policyBuilder =>
                    policyBuilder.CircuitBreakerAsync(
                        handledEventsAllowedBeforeBreaking: 5,
                        durationOfBreak: TimeSpan.FromSeconds(15)));
}

public static class HttpClients
{
    public const string EXTERNAL = "external";
}
