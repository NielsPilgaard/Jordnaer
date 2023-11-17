namespace Jordnaer.Server.Features.GroupSearch;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddGroupSearchServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IGroupSearchService, GroupSearchService>();

        return builder;
    }
}
