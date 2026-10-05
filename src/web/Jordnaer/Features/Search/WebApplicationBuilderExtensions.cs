namespace Jordnaer.Features.Search;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddSearchServices(this WebApplicationBuilder builder)
	{
		builder.Services.AddSingleton<IZipCodeService, ZipCodeService>();

		return builder;
	}
}
