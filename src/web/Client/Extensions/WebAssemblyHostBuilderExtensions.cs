using Polly;
using Refit;

namespace Jordnaer.Client;

public static class WebAssemblyHostBuilderExtensions
{
    public static IServiceCollection AddRefitClient<TClient>(
        this IServiceCollection services,
        Uri baseAddress,
        RefitSettings? settings = null)
        where TClient : class
    {
        services.AddRefitClient<TClient>(settings)
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = baseAddress;
                // This prevents the api from redirecting to Account/Login on 401
                client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            })
            .AddTransientHttpErrorPolicy(policyBuilder =>
                policyBuilder.WaitAndRetryAsync(3, retryCount => TimeSpan.FromMilliseconds(50 * retryCount)));

        return services;
    }

    public static IServiceCollection AddRefitClient<TClient>(
        this IServiceCollection services,
        string baseAddress,
        RefitSettings? settings = null) where TClient : class
        => services.AddRefitClient<TClient>(new Uri(baseAddress), settings);
}
