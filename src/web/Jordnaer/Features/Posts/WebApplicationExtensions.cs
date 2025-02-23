namespace Jordnaer.Features.Posts;

public static class WebApplicationExtensions
{
	public static WebApplicationBuilder AddPostFeature(this WebApplicationBuilder builder)
	{
		builder.Services.AddScoped<PostService>();

		return builder;
	}
}