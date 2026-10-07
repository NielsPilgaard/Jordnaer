using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;

namespace Jordnaer.Shared;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddAdressevaelgerClient(this IServiceCollection services)
	{
		services.AddOptions<AdressevaelgerOptions>()
				.BindConfiguration(AdressevaelgerOptions.SectionName)
				.ValidateDataAnnotations()
				.ValidateOnStart();

		services.AddTransient<AdressevaelgerTokenHandler>();

		services.AddRefitClient<IAdressevaelgerClient>()
				.ConfigureHttpClient((provider, client) =>
				{
					var options = provider.GetRequiredService<IOptions<AdressevaelgerOptions>>().Value;

					client.BaseAddress = new Uri(options.BaseUrl);
				})
				.AddHttpMessageHandler<AdressevaelgerTokenHandler>()
				.AddStandardResilienceHandler();

		return services;
	}
}
