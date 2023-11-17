using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Contrib.WaitAndRetry;
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
                policyBuilder.WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(500), 3)));

        return services;
    }
}
