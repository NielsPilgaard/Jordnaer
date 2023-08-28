using Jordnaer.Server.Database;
using MassTransit;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Azure.SignalR;

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

    public static WebApplicationBuilder AddAzureSignalR(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddSignalR()
            .AddAzureSignalR(options =>
            {
                options.ConnectionString = builder.Configuration.GetConnectionString("AzureSignalR");
                options.ServerStickyMode = ServerStickyMode.Required;
            });

        if (!builder.Environment.IsDevelopment())
        {
            builder.Services.AddResponseCompression(options =>
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/octet-stream" }));
        }

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
