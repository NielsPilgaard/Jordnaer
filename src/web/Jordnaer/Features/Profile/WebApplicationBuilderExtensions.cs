using Jordnaer.Extensions;

namespace Jordnaer.Features.Profile;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddProfileServices(this WebApplicationBuilder builder, string baseUrl)
	{
		builder.Services.AddRefitClient<IProfileClient>(baseUrl);
		builder.Services.AddScoped<IProfileCache, ProfileCache>();

		return builder;
	}
}