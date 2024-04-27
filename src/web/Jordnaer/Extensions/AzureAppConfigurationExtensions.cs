using Microsoft.FeatureManagement;

namespace Jordnaer.Extensions;

public static class AzureAppConfigurationExtensions
{
	public static WebApplicationBuilder AddAzureAppConfiguration(this WebApplicationBuilder builder)
	{
		if (builder.Environment.IsDevelopment())
		{
			return builder;
		}

		var connectionString = builder.Configuration.GetConnectionString("AppConfig");
		if (connectionString is null)
		{
			throw new InvalidOperationException("Failed to find connection string to Azure App Configuration. Keys checked: 'ConnectionStrings:AppConfig'");
		}

		builder.Services.AddFeatureManagement();
		builder.Services.AddAzureAppConfiguration();
		builder.Configuration.AddAzureAppConfiguration(options =>
			options.Connect(connectionString)
				// Load all keys that have no label
				.Select("*")
				.ConfigureRefresh(refreshOptions =>
					// Only reload configs if the 'Sentinel' key is modified
					refreshOptions.Register("Sentinel", refreshAll: true))
				.UseFeatureFlags());

		return builder;
	}
}
