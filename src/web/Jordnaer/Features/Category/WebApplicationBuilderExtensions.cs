namespace Jordnaer.Features.Category;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddCategoryServices(this WebApplicationBuilder builder)
	{
		builder.Services.AddScoped<ICategoryCache, CategoryCache>();

		return builder;
	}
}
