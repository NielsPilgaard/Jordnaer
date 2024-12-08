namespace Jordnaer.Features.Posts;

public static class WebApplicationExtensions
{
	public static WebApplicationBuilder AddPostService(this WebApplicationBuilder builder)
	{
		builder.Services.AddScoped<IPostService, PostService>();

		return builder;
	}
}