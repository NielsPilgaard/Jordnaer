using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Refit;

namespace Jordnaer.Shared;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDataForsyningenClient(this IServiceCollection services)
	{
		services.AddOptions<DataForsyningenOptions>()
				.BindConfiguration(DataForsyningenOptions.SectionName)
				.ValidateDataAnnotations()
				.ValidateOnStart();

		services.AddRefitClient<IDataForsyningenClient>()
				.ConfigureHttpClient((provider, client) =>
				{
					var options = provider.GetRequiredService<IOptions<DataForsyningenOptions>>().Value;

					client.BaseAddress = new Uri(options.BaseUrl);
				})
				.AddStandardResilienceHandler();

		var pingCircuitBreakerOptions = new HttpCircuitBreakerStrategyOptions
		{
			FailureRatio = 0.25,
			BreakDuration = TimeSpan.FromMinutes(1),
			ShouldHandle = arguments =>
				new ValueTask<bool>(
					arguments.Outcome.Exception is not null ||
					arguments.Outcome.Result?.IsSuccessStatusCode is false)
		};
		services.AddRefitClient<IDataForsyningenPingClient>()
				.ConfigureHttpClient((provider, client) =>
				{
					var options = provider.GetRequiredService<IOptions<DataForsyningenOptions>>().Value;

					client.BaseAddress = new Uri(options.BaseUrl);
				})
				// This resilience strategy will pause all pings for 5 minutes if the API returns an error.
				.AddResilienceHandler("ping",
									  builder => builder.AddCircuitBreaker(pingCircuitBreakerOptions)
														.AddTimeout(TimeSpan.FromSeconds(30)));

		return services;
	}
}
