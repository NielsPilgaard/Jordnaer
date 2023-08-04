using Jordnaer.Server.Database;
using MassTransit;

namespace Jordnaer.Server.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddMassTransit(this WebApplicationBuilder builder)
    {
        builder.Services.AddMassTransit(config =>
        {
            config.UsingAzureServiceBus((_, azureServiceBus) =>
                azureServiceBus.Host(builder.Configuration.GetConnectionString("AzureServiceBus"))
            );
        });

        return builder;
    }

    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        string dbConnectionString = GetConnectionString(builder.Configuration);

        builder.Services.AddSqlServer<JordnaerDbContext>(dbConnectionString);

        builder.Services.AddHealthChecks().AddSqlServer(dbConnectionString);

        return builder;
    }

    private static string GetConnectionString(IConfiguration configuration) =>
           Environment.GetEnvironmentVariable($"ConnectionStrings_{nameof(JordnaerDbContext)}")
        ?? Environment.GetEnvironmentVariable($"ConnectionStrings__{nameof(JordnaerDbContext)}")
        ?? configuration.GetConnectionString(nameof(JordnaerDbContext))
        ?? throw new InvalidOperationException(
            $"Connection string '{nameof(JordnaerDbContext)}' not found.");
}
