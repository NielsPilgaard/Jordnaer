namespace Jordnaer.Features.Ad;

public static class WebApplicationBuilderExtensions
{
	public static void AddAdServices(this WebApplicationBuilder builder)
	{
		builder.Services.AddScoped<IAdProvider, AdProvider>();
	}
}
