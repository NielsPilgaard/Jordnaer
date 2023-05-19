using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Polly;
using Refit;

namespace Jordnaer.Client;

public static class WebAssemblyHostBuilderExtensions
{

    public static IServiceCollection AddRefitClient<TClient>(this IServiceCollection services, Uri baseAddress)
        where TClient : class
    {
        services.AddRefitClient<TClient>().ConfigureHttpClient(client =>
                client.BaseAddress = baseAddress)
            .AddTransientHttpErrorPolicy(policyBuilder =>
                policyBuilder.WaitAndRetryAsync(3, retryCount => TimeSpan.FromMilliseconds(50 * retryCount)))
            .AddTransientHttpErrorPolicy(policyBuilder =>
                policyBuilder.CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromSeconds(10)));

        return services;
    }

    public static IServiceCollection AddRefitClient<TClient>(this IServiceCollection services,
        string baseAddress) where TClient : class => services.AddRefitClient<TClient>(new Uri(baseAddress));

    public static WebAssemblyHostBuilder AddResilientHttpClient(this WebAssemblyHostBuilder builder)
    {
        builder.Services.AddHttpClient(HttpClients.INTERNAL_API,
                client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
            .AddTransientHttpErrorPolicy(policyBuilder =>
                policyBuilder.WaitAndRetryAsync(3, retryCount => TimeSpan.FromMilliseconds(50 * retryCount)))
            .AddTransientHttpErrorPolicy(policyBuilder =>
                policyBuilder.CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromSeconds(10)));

        builder.Services.AddScoped(sp =>
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClients.INTERNAL_API));

        return builder;
    }
}

public static class HttpClients
{
    public const string INTERNAL_API = "internal_api";
}
