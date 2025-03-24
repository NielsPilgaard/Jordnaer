using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.ElmahIo;
using Serilog.Sinks.Grafana.Loki;
using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared.Infrastructure;

public static class SerilogExtensions
{
	public static WebApplicationBuilder AddSerilog(this WebApplicationBuilder builder)
	{
		if (!builder.Environment.IsDevelopment())
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
		}

		builder.Host.UseSerilog((context, provider, loggerConfiguration) =>
		{
			loggerConfiguration.ReadFrom.Configuration(context.Configuration)
				.Enrich.WithProperty("Application", builder.Environment.ApplicationName)
				.Enrich.FromLogContext()
				.Enrich.WithExceptionDetails();

			loggerConfiguration.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level}] {SourceContext}: {Message:lj}{NewLine}{Exception}");

			if (context.HostingEnvironment.IsDevelopment())
			{
				return;
			}

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
				_ => LogEventLevel.Information
			});

	/// <summary>
	/// Write to elmah.io if not in development, and the log level is warning or higher
	/// </summary>
	/// <param name="loggerConfiguration"></param>
	/// <param name="provider"></param>
	private static void WriteToElmahIo(this LoggerConfiguration loggerConfiguration,
		IServiceProvider provider)
	{
		var elmahIoOptions = provider.GetRequiredService<IOptions<ElmahIoOptions>>().Value;

		loggerConfiguration
			.WriteTo
			.Conditional(logEvent => logEvent.Level >= LogEventLevel.Error,
						 configuration => configuration.ElmahIo(
							 new ElmahIoSinkOptions(elmahIoOptions.ApiKey,
													elmahIoOptions.LogIdGuid)));
	}

	private static void WriteToLoki(this LoggerConfiguration loggerConfiguration,
		IServiceProvider provider)
	{
		var grafanaLokiOptions = provider.GetRequiredService<IOptions<GrafanaLokiOptions>>().Value;

		var configuration = provider.GetRequiredService<IConfiguration>();

		var environment = configuration["ENVIRONMENT"] ??
						  configuration["ASPNETCORE_ENVIRONMENT"] ??
						  configuration["DOTNET_ENVIRONMENT"] ??
						  "Not Configured";

		var labels = new LokiLabel[] {
				new()
				{
					Key = "environment",
					Value = environment
				},
				new() { Key="service_name", Value = "Jordnaer" }
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

	[Required]
	public required string ApiKey { get; set; }

	[Required]
	public required string LogId { get; set; }

	public Guid LogIdGuid => new(LogId);
}

public sealed class GrafanaLokiOptions
{
	public const string SectionName = "GrafanaLoki";

	[Url]
	public string Uri { get; set; } = null!;

	[Required]
	public string Login { get; set; } = null!;

	[Required]
	public string Password { get; set; } = null!;

	public int QueueLimit { get; set; } = 500;
}
