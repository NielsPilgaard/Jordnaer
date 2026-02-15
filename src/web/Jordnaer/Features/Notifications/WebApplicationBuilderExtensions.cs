namespace Jordnaer.Features.Notifications;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddNotificationServices(this WebApplicationBuilder builder)
	{
		builder.Services.AddScoped<INotificationSettingsService, NotificationSettingsService>();
		builder.Services.AddScoped<INotificationService, NotificationService>();
		builder.Services.AddScoped<NotificationSignalRClient>();
		builder.Services.AddScoped<INotificationCleanupService, NotificationCleanupService>();
		builder.Services.AddHostedService<NotificationCleanupBackgroundService>();
		return builder;
	}
}
