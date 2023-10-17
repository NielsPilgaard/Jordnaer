using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Jordnaer.Shared.Infrastructure;

public static class AzureAppConfigurationExtensions
{
    public static WebApplicationBuilder AddAzureAppConfiguration(this WebApplicationBuilder builder, string configKey = "ConnectionStrings:AppConfig")
    {
        string? connectionString = builder.Configuration[configKey];
        if (connectionString is null)
        {
            return builder.Environment.IsDevelopment()
                ? builder
                : throw new InvalidOperationException("Connection string 'AppConfig' not found.");
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
