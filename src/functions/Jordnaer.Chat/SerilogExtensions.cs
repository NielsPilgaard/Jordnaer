using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Exceptions;
using Serilog.Sinks.Grafana.Loki;

namespace Jordnaer.Chat;

public static class SerilogExtensions
{
    private const string ApplicationName = "Jordnaer.Chat";

    public static void AddSerilog(this IConfiguration configuration) =>
        Log.Logger = new LoggerConfiguration()
            .Enrich.WithProperty("Application", ApplicationName)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteToLoki(configuration)
            .CreateLogger();

    private static LoggerConfiguration WriteToLoki(this LoggerConfiguration loggerConfiguration,
        IConfiguration configuration)
    {
        var grafanaLokiOptions = new GrafanaLokiOptions();
        configuration.GetSection(GrafanaLokiOptions.SectionName).Bind(grafanaLokiOptions);

        Validator.ValidateObject(grafanaLokiOptions, new ValidationContext(grafanaLokiOptions));

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


public sealed class GrafanaLokiOptions
{
    public const string SectionName = "GrafanaLoki";

    [Required]
    [Url]
    public string Uri { get; set; } = null!;

    [Required]
    public string Login { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;

    public int QueueLimit { get; set; } = 500;
}
