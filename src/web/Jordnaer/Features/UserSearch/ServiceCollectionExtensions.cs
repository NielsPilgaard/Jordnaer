using Jordnaer.Shared;

namespace Jordnaer.Server.Features.UserSearch;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserSearchFeature(this IServiceCollection services)
    {
        services.AddOptions<DataForsyningenOptions>()
            .BindConfiguration(DataForsyningenOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IUserSearchService, UserSearchService>();

        services.AddDataForsyningenClient();

        services.AddHealthChecks().AddCheck<DataForsyningenHealthCheck>("dataforsyningen", tags: new[] { "external", "api" });

        return services;
    }
}
