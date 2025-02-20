namespace Jordnaer.Features.PostSearch;

public static class WebApplicationExtensions
{
	public static WebApplicationBuilder AddPostSearchService(this WebApplicationBuilder builder)
	{
		builder.Services.AddScoped<IPostSearchService, PostSearchService>();

		return builder;
	}
}