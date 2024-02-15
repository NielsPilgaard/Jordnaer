using Jordnaer.Database;
using MassTransit;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;

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
		builder.Services.AddSignalR(options =>
		{
			options.ClientTimeoutInterval = TimeSpan.FromMinutes(5);
			options.KeepAliveInterval = TimeSpan.FromMinutes(1);
		});


		builder.Services.AddResponseCompression(options =>
			options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]));


		return builder;
	}

	public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
	{
		var dbConnectionString = GetConnectionString(builder.Configuration);

		builder.Services.AddDbContextFactory<JordnaerDbContext>(optionsBuilder => optionsBuilder.UseSqlServer(dbConnectionString));

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
