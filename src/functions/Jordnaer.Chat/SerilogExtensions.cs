using Jordnaer.Shared.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Exceptions;
using Serilog.Sinks.Grafana.Loki;

namespace Jordnaer.Chat;

public static class SerilogExtensions
{
    private const string ApplicationName = "Jordnaer.Chat";

    public static IServiceCollection AddSerilog(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GrafanaLokiOptions>()
            .BindConfiguration("GrafanaLoki")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.WithProperty("Application", ApplicationName)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteToLoki(configuration)
            .CreateLogger();

        services.AddLogging(loggingBuilder => loggingBuilder.ClearProviders().AddSerilog(logger));

        return services;
    }

    private static LoggerConfiguration WriteToLoki(this LoggerConfiguration loggerConfiguration,
        IConfiguration configuration)
    {
        var grafanaLokiOptions = new GrafanaLokiOptions();
        configuration.GetSection(GrafanaLokiOptions.SectionName).Bind(grafanaLokiOptions);

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

        return loggerConfiguration;
    }
}
