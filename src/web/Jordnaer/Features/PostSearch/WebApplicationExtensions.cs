namespace Jordnaer.Features.PostSearch;

public static class WebApplicationExtensions
{
	public static WebApplicationBuilder AddPostSearchFeature(this WebApplicationBuilder builder)
	{
		builder.Services.AddScoped<PostSearchService>();

		return builder;
	}
}