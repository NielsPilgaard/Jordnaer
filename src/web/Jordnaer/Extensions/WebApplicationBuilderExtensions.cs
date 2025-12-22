using Grafana.OpenTelemetry;
using HealthChecks.OpenTelemetry.Instrumentation;
using Jordnaer.Database;
using MassTransit;
using MassTransit.Logging;
using MassTransit.Monitoring;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using MudBlazor.Services;
using MudExtensions.Services;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;

namespace Jordnaer.Extensions;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddMassTransit(this WebApplicationBuilder builder)
	{
		builder.Services.AddMassTransit(x =>
		{
			x.AddConsumersFromNamespaceContaining<Program>();

			x.UsingInMemory((context, busConfigurator) =>
			{
				busConfigurator.UseInMemoryOutbox(context);
				busConfigurator.ConfigureEndpoints(context);
			});
		});

		return builder;
	}

	public static WebApplicationBuilder AddSignalR(this WebApplicationBuilder builder)
	{
		builder.Services.AddResponseCompression(options =>
			options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]));

		return builder;
	}

	public static WebApplicationBuilder AddMudBlazor(this WebApplicationBuilder builder)
	{
		builder.Services.AddMudServices(configuration =>
		{
			configuration.ResizeOptions = new ResizeOptions
			{
				NotifyOnBreakpointOnly = true
			};
			configuration.SnackbarConfiguration = new SnackbarConfiguration
			{
				VisibleStateDuration = 2500,
				ShowTransitionDuration = 250,
				BackgroundBlurred = true,
				MaximumOpacity = 95,
				MaxDisplayedSnackbars = 3,
				PositionClass = Defaults.Classes.Position.BottomCenter,
				HideTransitionDuration = 100,
				ShowCloseIcon = false
			};
		});
		builder.Services.AddMudExtensions();

		return builder;
	}
	public static WebApplicationBuilder AddOpenTelemetry(this WebApplicationBuilder builder)
	{
		const string serviceName = "Jordnaer";

		var openTelemetryBuilder = builder.Services
			   .AddOpenTelemetry()
			   .ConfigureResource(resource => resource
				   .AddService(serviceName)
				   .AddAttributes(new Dictionary<string, object>
				   {
					   ["environment"] = builder.Environment.EnvironmentName,
					   ["version"] = GetVersion()
				   }))
			   .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation()
											  .AddHealthChecksInstrumentation(options => options.IncludeHealthCheckMetadata = true)
											  .AddHttpClientInstrumentation()
											  .AddProcessInstrumentation()
											  .AddRuntimeInstrumentation()
											  .AddMeter(InstrumentationOptions.MeterName)
											  .AddMeter("Polly")
											  .AddMeter("Microsoft.EntityFrameworkCore")
											  .AddMeter(serviceName))
			   .WithTracing(tracing =>
			   {
				   tracing.AddAspNetCoreInstrumentation(options =>
						  {
							  // Filter out health checks and other noisy paths to keep traces clean
							  options.Filter = httpContext =>
							  {
								  var path = httpContext.Request.Path.Value;
								  return path is not (null or "/health" or "/healthy" or "/alive" or "/favicon.ico");
							  };
							  options.RecordException = true;
						  })
						  .AddHttpClientInstrumentation()
						  .AddSqlClientInstrumentation()
						  .AddEntityFrameworkCoreInstrumentation()
						  .AddSource(DiagnosticHeaders.DefaultListenerName)
						  .AddSource("Polly")
						  .AddSource(serviceName);
			   });

		// Use the Aspire Dashboard in development
		if (builder.Environment.IsDevelopment())
		{
			openTelemetryBuilder.UseOtlpExporter();
			builder.Logging.AddOpenTelemetry(logging =>
			{
				logging.IncludeFormattedMessage = true;
				logging.IncludeScopes = true;
			});
		}
		else // Use Grafana in production
		{
			openTelemetryBuilder.UseGrafana();
		}

		return builder;
	}

	public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
	{
		var connectionString = GetConnectionString(builder.Configuration);

		builder.Services.AddDbContextFactory<JordnaerDbContext>(
				   optionsBuilder => optionsBuilder
					   .UseSqlServer(connectionString, sqlOptions => sqlOptions.UseNetTopologySuite()),
				   ServiceLifetime.Scoped);

		builder.Services.AddHealthChecks().AddSqlServer(connectionString);

		return builder;
	}

	private static string GetConnectionString(IConfiguration configuration) =>
		configuration.GetConnectionString(nameof(JordnaerDbContext))
		?? throw new InvalidOperationException(
			$"Connection string '{nameof(JordnaerDbContext)}' not found.");

	private static string GetVersion()
	{
		var informationalVersion = typeof(Program).Assembly
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

		if (string.IsNullOrWhiteSpace(informationalVersion))
		{
			return typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0";
		}

		// If the version contains a +, it's a git commit hash suffix, let's keep it or strip it?
		// Usually informational version is "1.7.2+commitHash" or just "1.7.2"
		// If it's the full tag "release-website-v1.7.2", we might want to clean it.
		const string prefix = "release-website-v";
		if (informationalVersion.StartsWith(prefix))
		{
			informationalVersion = informationalVersion[prefix.Length..];
		}

		return informationalVersion;
	}
}
