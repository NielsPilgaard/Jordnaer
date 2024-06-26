namespace Jordnaer.Features.Search;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddSearchServices(this WebApplicationBuilder builder)
	{
		builder.Services.AddScoped<IZipCodeService, ZipCodeService>();

		return builder;
	}
}