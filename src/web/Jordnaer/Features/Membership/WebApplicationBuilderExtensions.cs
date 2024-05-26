namespace Jordnaer.Features.Membership;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddMembershipServices(this WebApplicationBuilder builder)
	{
		builder.Services.AddScoped<IMembershipService, MembershipService>();

		return builder;
	}
}
