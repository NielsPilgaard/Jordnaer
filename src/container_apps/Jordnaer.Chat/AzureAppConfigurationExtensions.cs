namespace Jordnaer.Chat;

public static class AzureAppConfigurationExtensions
{
    public static WebApplicationBuilder AddAzureAppConfiguration(this WebApplicationBuilder builder)
    {
        string? connectionString = builder.Configuration.GetConnectionString("AppConfig") ?? builder.Configuration["appconfig"];
        if (connectionString is null)
        {
            throw new InvalidOperationException("Failed to find connection string to Azure App Configuration. Keys checked: 'ConnectionStrings:AppConfig' and 'appconfig'");
        }

        builder.Services.AddAzureAppConfiguration();
        builder.Configuration.AddAzureAppConfiguration(options =>
            options.Connect(connectionString)
                // Load all keys that have no label
                .Select("*")
                // Configure to reload builder if the registered sentinel key is modified
                .ConfigureRefresh(refreshOptions =>
                {
                    refreshOptions.Register("Sentinel", refreshAll: true);
                    refreshOptions.SetCacheExpiration(TimeSpan.FromMinutes(5));
                })
                .UseFeatureFlags(flagOptions => flagOptions.CacheExpirationInterval = TimeSpan.FromMinutes(3)));

        return builder;
    }
}
