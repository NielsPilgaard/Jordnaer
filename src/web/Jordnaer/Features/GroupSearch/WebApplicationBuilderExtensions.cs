using Jordnaer.Extensions;

namespace Jordnaer.Features.GroupSearch;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddGroupSearchServices(this WebApplicationBuilder builder, string baseUrl)
	{
		builder.Services.AddScoped<IGroupSearchService, GroupSearchService>();

		builder.Services.AddRefitClient<IGroupSearchClient>(baseUrl);

		return builder;
	}
}
