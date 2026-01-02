namespace Jordnaer.Features.Notifications;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddNotificationServices(this WebApplicationBuilder builder)
	{
		builder.Services.AddScoped<INotificationSettingsService, NotificationSettingsService>();
		return builder;
	}
}
