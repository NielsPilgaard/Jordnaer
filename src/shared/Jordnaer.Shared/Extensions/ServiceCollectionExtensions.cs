using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

		return services;
	}
}
