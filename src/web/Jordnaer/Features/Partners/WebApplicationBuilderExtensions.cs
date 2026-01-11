namespace Jordnaer.Features.Partners;

public static class WebApplicationBuilderExtensions
{
	public static void AddPartnerServices(this WebApplicationBuilder builder)
	{
		builder.Services.AddScoped<IPartnerUserService, PartnerUserService>();
		builder.Services.AddScoped<IPartnerService, PartnerService>();
	}
}
