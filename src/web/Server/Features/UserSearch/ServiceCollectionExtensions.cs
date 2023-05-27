using Jordnaer.Shared.UserSearch;

namespace Jordnaer.Server.Features.UserSearch;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserSearchServices(this IServiceCollection services)
    {
        services.AddOptions<DataForsyningenOptions>()
            .BindConfiguration(DataForsyningenOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IUserSearchService, UserSearchService>();

        services.AddDataForsyningenClient();

        return services;
    }
}
