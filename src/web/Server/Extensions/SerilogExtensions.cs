using Jordnaer.Shared.Infrastructure;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Exceptions;
using Serilog.Sinks.Grafana.Loki;

namespace Jordnaer.Server.Extensions;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddSerilog(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddOptions<GrafanaLokiOptions>()
            .BindConfiguration(GrafanaLokiOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Host.UseSerilog((context, provider, loggerConfiguration) =>
        {
            loggerConfiguration.ReadFrom.Configuration(context.Configuration)
                .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails();

            loggerConfiguration.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level}] {SourceContext}: {Message:lj}{NewLine}{Exception}");

            loggerConfiguration.WriteToLoki(provider);
        });

        return builder;
    }

    private static void WriteToLoki(this LoggerConfiguration loggerConfiguration,
        IServiceProvider provider)
    {
        var grafanaLokiOptions = provider.GetRequiredService<IOptions<GrafanaLokiOptions>>().Value;

        var labels =
            new LokiLabel[] {
                new()
                {
                    Key = "environment_machine_name",
                    Value = Environment.MachineName
                },
                new()
                {
                    Key = "environment",
                    Value = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Not Configured"
                }
            };

        loggerConfiguration.WriteTo.GrafanaLoki(
            uri: grafanaLokiOptions.Uri,
            textFormatter: new LokiJsonTextFormatter(),
            labels: labels,
            credentials: new LokiCredentials
            {
                Login = grafanaLokiOptions.Login,
                Password = grafanaLokiOptions.Password
            },
            queueLimit: grafanaLokiOptions.QueueLimit);
    }
}
