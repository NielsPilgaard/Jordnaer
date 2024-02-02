namespace Jordnaer.Features.Groups;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddGroupServices(this WebApplicationBuilder builder)
	{
		builder.Services.AddScoped<IGroupService, GroupService>();

		return builder;
	}
}
