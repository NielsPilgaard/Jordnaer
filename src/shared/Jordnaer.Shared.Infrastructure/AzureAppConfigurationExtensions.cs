using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;

namespace Jordnaer.Shared.Infrastructure;

public static class AzureAppConfigurationExtensions
{
	public static WebApplicationBuilder AddAzureAppConfiguration(this WebApplicationBuilder builder)
	{
		builder.Services.AddFeatureManagement();

		if (builder.Environment.IsDevelopment())
		{
			return builder;
		}


		var connectionString = builder.Configuration.GetConnectionString("")
							   ?? throw new InvalidOperationException(
								   "Failed to find connection string to Azure App Configuration. " +
								   "Keys checked: 'ConnectionStrings:AppConfig'");

		builder.Configuration.AddAzureAppConfiguration(options =>
			options.Connect(connectionString)
				// Load all keys that have no label
				.Select("*")
				.ConfigureRefresh(refreshOptions =>
					// Only reload configs if the 'Sentinel' key is modified
					refreshOptions.Register("Sentinel", refreshAll: true))
				.UseFeatureFlags());

		builder.Services.AddAzureAppConfiguration();

		return builder;
	}
}
