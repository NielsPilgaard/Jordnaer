namespace Jordnaer.Features.Profile;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddProfileServices(this WebApplicationBuilder builder)
	{
		builder.Services.AddScoped<IProfileCache, ProfileCache>();
		builder.Services.AddScoped<IProfileService, ProfileService>();
		builder.Services.AddScoped<IProfileImageService, ProfileImageService>();

		return builder;
	}
}
