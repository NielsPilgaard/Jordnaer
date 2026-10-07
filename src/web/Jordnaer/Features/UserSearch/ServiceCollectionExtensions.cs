namespace Jordnaer.Features.UserSearch;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddUserSearchFeature(this IServiceCollection services)
	{
		services.AddScoped<IUserSearchService, UserSearchService>();

		return services;
	}
}
