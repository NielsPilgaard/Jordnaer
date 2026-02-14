namespace Jordnaer.Features.Groups;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddGroupServices(this WebApplicationBuilder builder)
	{
		builder.Services.AddScoped<GroupService>();

		return builder;
	}
}
