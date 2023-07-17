using Jordnaer.Chat;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Jordnaer.Chat;

internal class Startup : FunctionsStartup
{
    private IConfiguration _configuration = null!;

    public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
    {
        _configuration = builder.ConfigurationBuilder
            .AddEnvironmentVariables()
            .AddUserSecrets<Startup>()
            .Build();

        string connectionString = _configuration.GetConnectionString("AppConfig")
                                  ?? throw new InvalidOperationException("Connection string 'AppConfig' not found.");

        builder.ConfigurationBuilder.AddAzureAppConfiguration(options =>
            options.Connect(connectionString)
                // Load all keys that have no label
                .Select("*")
                // Configure to reload builder if the registered sentinel key is modified
                .ConfigureRefresh(refreshOptions =>
                {
                    refreshOptions.Register("Sentinel", refreshAll: true);
                    refreshOptions.SetCacheExpiration(TimeSpan.FromMinutes(5));
                }));
    }

    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddSerilog(_configuration);
        builder.Services.AddAzureAppConfiguration();
    }
}
