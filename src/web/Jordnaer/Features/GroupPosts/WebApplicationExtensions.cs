namespace Jordnaer.Features.GroupPosts;

public static class WebApplicationExtensions
{
	public static WebApplicationBuilder AddGroupPostFeature(this WebApplicationBuilder builder)
	{
		builder.Services.AddScoped<GroupPostService>();

		return builder;
	}
}
