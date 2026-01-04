namespace Jordnaer.Features.Sponsors;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddSponsorServices(this WebApplicationBuilder builder)
	{
		builder.Services.AddScoped<ISponsorService, SponsorService>();
		return builder;
	}
}
