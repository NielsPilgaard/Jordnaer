using MassTransit;

namespace Jordnaer.Chat;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddMassTransit(this WebApplicationBuilder builder)
	{
		builder.Services.AddMassTransit(config =>
		{
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
		var connectionString = GetConnectionString(builder.Configuration);

		builder.Services
			   .AddSqlServer<JordnaerDbContext>(connectionString,
												optionsBuilder => optionsBuilder.UseAzureSqlDefaults());

		builder.Services.AddHealthChecks().AddSqlServer(connectionString);

		return builder;
	}

	private static string GetConnectionString(IConfiguration configuration) =>
		configuration.GetConnectionString(nameof(JordnaerDbContext))
		?? throw new InvalidOperationException(
			$"Connection string '{nameof(JordnaerDbContext)}' not found.");
}
