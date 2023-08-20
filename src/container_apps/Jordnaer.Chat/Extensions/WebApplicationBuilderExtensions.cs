using MassTransit;

namespace Jordnaer.Chat;

//TODO: Implement

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddMassTransit(this WebApplicationBuilder builder)
    {
        builder.Services.AddMassTransit(config =>
        {
            config.AddConsumer<StartChatConsumer>();
            config.AddConsumer<SendMessageConsumer>();
            config.AddConsumer<SetChatNameConsumer>();

            config.UsingAzureServiceBus((context, azureServiceBus) =>
            {
                azureServiceBus.Host(builder.Configuration.GetConnectionString("AzureServiceBus"));

                azureServiceBus.ConfigureEndpoints(context);
            });
        });

        return builder;
    }

    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        string dbConnectionString = GetConnectionString(builder.Configuration);

        builder.Services.AddSqlServer<ChatDbContext>(dbConnectionString);

        builder.Services.AddHealthChecks().AddSqlServer(dbConnectionString);

        return builder;
    }

    private static string GetConnectionString(IConfiguration configuration) =>
        Environment.GetEnvironmentVariable("ConnectionStrings_JordnaerDbContext")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__JordnaerDbContext")
        ?? configuration.GetConnectionString("JordnaerDbContext")
        ?? throw new InvalidOperationException(
            $"Connection string 'JordnaerDbContext' not found.");
}
