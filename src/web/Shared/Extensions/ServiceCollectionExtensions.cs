using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Refit;

namespace Jordnaer.Shared;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataForsyningenClient(this IServiceCollection services)
    {
        services.AddRefitClient<IDataForsyningenClient>()
            .ConfigureHttpClient((provider, client) =>
            {
                var dataForsyningenOptions = provider.GetRequiredService<IOptions<DataForsyningenOptions>>().Value;

                client.BaseAddress = new Uri(dataForsyningenOptions.BaseUrl);
            })
            .AddTransientHttpErrorPolicy(policyBuilder =>
                policyBuilder.WaitAndRetryAsync(3, retryCount => TimeSpan.FromMilliseconds(350 * retryCount)))
            .AddTransientHttpErrorPolicy(policyBuilder =>
                policyBuilder.CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(15)));

        return services;
    }
}
