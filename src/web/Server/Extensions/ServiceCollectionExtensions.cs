using Jordnaer.Server.Features.Search;

namespace Jordnaer.Server.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserSearchServices(this IServiceCollection services)
    {
        services.AddScoped<IUserSearchService, UserSearchService>();

        return services;
    }
}
