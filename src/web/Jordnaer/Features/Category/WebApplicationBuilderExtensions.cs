using Jordnaer.Extensions;

namespace Jordnaer.Features.Category;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddCategoryServices(this WebApplicationBuilder builder, string baseUrl)
	{
		builder.Services.AddRefitClient<ICategoryClient>(baseUrl);
		builder.Services.AddSingleton<ICategoryCache, CategoryCache>();

		return builder;
	}
}
