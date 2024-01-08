using Jordnaer.Extensions;

namespace Jordnaer.Features.Groups;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddGroupServices(this WebApplicationBuilder builder, string baseUrl)
	{
		builder.Services.AddScoped<IGroupService, GroupService>();

		builder.Services.AddRefitClient<IGroupClient>(baseUrl);
		builder.Services.AddRefitClient<IGroupChatClient>(baseUrl);

		return builder;
	}
}
