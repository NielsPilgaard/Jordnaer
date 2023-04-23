namespace Jordnaer.Server.Extensions;

public static class AzureAppConfigurationExtensions
{
    public static WebApplicationBuilder AddAzureAppConfiguration(this WebApplicationBuilder builder)
    {
        string appConfigConnectionString = builder.Configuration.GetConnectionString("AppConfig") ??
                                           throw new InvalidOperationException("Connection string 'AppConfig' not found.");

        builder.Configuration.AddAzureAppConfiguration(options =>
        {
            options.Connect(appConfigConnectionString)
                // Load all keys that have no label
                .Select("*")
                // Configure to reload builder if the registered sentinel key is modified
                .ConfigureRefresh(refreshOptions =>
                {
                    refreshOptions.Register("Sentinel", refreshAll: true);
                    refreshOptions.SetCacheExpiration(TimeSpan.FromMinutes(5));
                })
                .UseFeatureFlags(flagOptions => flagOptions.CacheExpirationInterval = TimeSpan.FromMinutes(3));
        });

        builder.Services.AddAzureAppConfiguration();

        return builder;
    }
}
