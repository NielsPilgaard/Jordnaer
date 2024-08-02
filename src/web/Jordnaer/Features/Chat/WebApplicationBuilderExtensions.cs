namespace Jordnaer.Features.Chat;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddChatServices(this WebApplicationBuilder builder)
	{
		builder.Services.AddScoped<IChatMessageCache, ChatMessageCache>();
		builder.Services.AddScoped<IChatService, ChatService>();
		builder.Services.AddScoped<ChatSignalRClient>();
		builder.Services.AddScoped<UnreadMessageSignalRClient>();
		builder.Services.AddScoped<ChatNotificationService>();
		return builder;
	}
}
