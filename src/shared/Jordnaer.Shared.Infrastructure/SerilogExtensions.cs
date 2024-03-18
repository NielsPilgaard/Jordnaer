using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.ElmahIo;
using Serilog.Sinks.Grafana.Loki;

namespace Jordnaer.Shared.Infrastructure;

public static class SerilogExtensions
{
	public static WebApplicationBuilder AddSerilog(this WebApplicationBuilder builder)
	{
		builder.Services
			.AddOptions<GrafanaLokiOptions>()
			.BindConfiguration(GrafanaLokiOptions.SectionName)
			.ValidateDataAnnotations()
			.ValidateOnStart();

		builder.Services
			   .AddOptions<ElmahIoOptions>()
			   .BindConfiguration(ElmahIoOptions.SectionName)
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
			loggerConfiguration.WriteToElmahIo(provider);
		});

		return builder;
	}

	public static IApplicationBuilder UseSerilog(this WebApplication app) =>
		app.UseSerilogRequestLogging(options => options.GetLevel = (context, _, exception) =>
			context.Response.StatusCode switch
			{
				>= 500 when exception is not null => LogEventLevel.Error,
				_ when exception is not null => LogEventLevel.Error,
				>= 400 => LogEventLevel.Warning,
				_ => app.Environment.IsDevelopment() ? LogEventLevel.Information : LogEventLevel.Debug
			});

	private static void WriteToElmahIo(this LoggerConfiguration loggerConfiguration,
		IServiceProvider provider)
	{
		var elmahIoOptions = provider.GetRequiredService<IOptions<ElmahIoOptions>>().Value;

		loggerConfiguration.WriteTo.ElmahIo(
			new ElmahIoSinkOptions(elmahIoOptions.ApiKey,
									elmahIoOptions.LogIdGuid));
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

public sealed class ElmahIoOptions
{
	public const string SectionName = "ElmahIo";

	[Required(ErrorMessage = "Påkrævet.")]
	public required string ApiKey { get; set; }

	[Required(ErrorMessage = "Påkrævet.")]
	public required string LogId { get; set; }

	public Guid LogIdGuid => new(LogId);
}

public sealed class GrafanaLokiOptions
{
	public const string SectionName = "GrafanaLoki";

	[Required(ErrorMessage = "Påkrævet.")]
	[Url]
	public string Uri { get; set; } = null!;

	[Required(ErrorMessage = "Påkrævet.")]
	public string Login { get; set; } = null!;

	[Required(ErrorMessage = "Påkrævet.")]
	public string Password { get; set; } = null!;

	public int QueueLimit { get; set; } = 500;
}
