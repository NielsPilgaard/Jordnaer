using Grafana.OpenTelemetry;
using Jordnaer.Components.Account;
using Jordnaer.Database;
using Jordnaer.Features.Authentication;
using MassTransit;
using MassTransit.Logging;
using MassTransit.Monitoring;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using MudBlazor.Services;
using MudExtensions.Services;
using OpenTelemetry.Metrics;

namespace Jordnaer.Extensions;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddMassTransit(this WebApplicationBuilder builder)
	{
		builder.Services.AddMassTransit(x =>
		{
			x.AddConsumersFromNamespaceContaining<Program>();

			if (builder.Environment.IsDevelopment())
			{
				x.SetEndpointNameFormatter(endpointNameFormatter:
										   new DefaultEndpointNameFormatter(prefix: "dev-"));
			}

			x.UsingAzureServiceBus((context, azureServiceBus) =>
			{
				if (builder.Environment.IsDevelopment())
				{
					azureServiceBus
						.MessageTopology
						.SetEntityNameFormatter(
							new PrefixEntityNameFormatter(AzureBusFactory.MessageTopology.EntityNameFormatter, "dev-"));
				}

				azureServiceBus.Host(builder.Configuration.GetConnectionString("AzureServiceBus"));

				azureServiceBus.UseInMemoryOutbox(context);

				azureServiceBus.ConfigureEndpoints(context);
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
		builder.Services
			   .AddOpenTelemetry()
			   .WithMetrics(x => x.AddMeter(InstrumentationOptions.MeterName)
								  .AddMeter("Polly")
								  .AddPrometheusExporter())
			   .WithTracing(x => x.AddSource(DiagnosticHeaders.DefaultListenerName))
			   .UseGrafana(options =>
			   {
				   options.Instrumentations.Clear();
				   options.Instrumentations.Add(Instrumentation.AspNetCore);
				   options.Instrumentations.Add(Instrumentation.EntityFrameworkCore);
				   options.Instrumentations.Add(Instrumentation.HttpClient);
				   options.Instrumentations.Add(Instrumentation.NetRuntime);
				   options.Instrumentations.Add(Instrumentation.Process);
				   options.Instrumentations.Add(Instrumentation.SqlClient);
			   });

		return builder;
	}
	public static WebApplicationBuilder AddAuthentication(this WebApplicationBuilder builder)
	{
		builder.Services.AddCascadingAuthenticationState();
		builder.Services.AddScoped<IdentityUserAccessor>();
		builder.Services.AddScoped<IdentityRedirectManager>();
		builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
		builder.Services.AddCurrentUser();

		builder.Services.AddAuthentication(options =>
		{
			options.DefaultScheme = IdentityConstants.ApplicationScheme;
			options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
		}).AddFacebook(options =>
		{
			options.AppId = builder.Configuration.GetValue<string>("Authentication:Schemes:Facebook:AppId")!;
			options.AppSecret = builder.Configuration.GetValue<string>("Authentication:Schemes:Facebook:AppSecret")!;
			options.SaveTokens = true;
		}).AddMicrosoftAccount(options =>
		{
			options.ClientId = builder.Configuration.GetValue<string>("Authentication:Schemes:Microsoft:ClientId")!;
			options.ClientSecret = builder.Configuration.GetValue<string>("Authentication:Schemes:Microsoft:ClientSecret")!;
			options.SaveTokens = true;
		}).AddGoogle(options =>
		{
			options.ClientId = builder.Configuration.GetValue<string>("Authentication:Schemes:Google:ClientId")!;
			options.ClientSecret = builder.Configuration.GetValue<string>("Authentication:Schemes:Google:ClientSecret")!;
			options.SaveTokens = true;
		}).AddIdentityCookies();

		builder.Services.AddIdentityCore<ApplicationUser>(options =>
			   {
				   options.SignIn.RequireConfirmedAccount = true;
				   options.User.RequireUniqueEmail = true;
			   })
			   .AddEntityFrameworkStores<JordnaerDbContext>()
			   .AddSignInManager()
			   .AddDefaultTokenProviders();

		return builder;
	}

	public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
	{
		var dbConnectionString = GetConnectionString(builder.Configuration);

		builder.Services.AddDbContextFactory<JordnaerDbContext>(
			optionsBuilder => optionsBuilder
				.UseSqlServer(dbConnectionString,
							  contextOptionsBuilder => contextOptionsBuilder.UseAzureSqlDefaults()),
			ServiceLifetime.Scoped);

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
