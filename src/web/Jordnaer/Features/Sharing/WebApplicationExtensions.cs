namespace Jordnaer.Features.Sharing;

public static class WebApplicationExtensions
{
	public static WebApplicationBuilder AddSharingFeature(this WebApplicationBuilder builder)
	{
		builder.Services.AddScoped<SocialShareService>();

		return builder;
	}
}
