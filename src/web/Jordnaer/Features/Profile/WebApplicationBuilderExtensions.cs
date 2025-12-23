using Jordnaer.Features.Images;

namespace Jordnaer.Features.Profile;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddProfileServices(this WebApplicationBuilder builder)
	{
		builder.Services.AddScoped<IProfileCache, ProfileCache>();
		builder.Services.AddScoped<IProfileService, ProfileService>();
		builder.Services.AddScoped<IProfileImageService, ProfileImageService>();
		builder.Services.AddScoped<ILocationService, LocationService>();

		builder.Services.AddScoped<IExternalProviderPictureDownloader, MicrosoftPictureDownloader>();
		builder.Services.AddScoped<IExternalProviderPictureDownloader, FacebookPictureDownloader>();
		builder.Services.AddScoped<IExternalProviderPictureDownloader, GooglePictureDownloader>();

		return builder;
	}
}
