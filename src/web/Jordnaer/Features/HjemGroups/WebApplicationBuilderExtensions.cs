namespace Jordnaer.Features.HjemGroups;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddHjemGroupServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IHjemGroupProvider, HjemGroupProvider>();
        builder.Services.AddScoped<HjemGroupAdminService>();

        return builder;
    }
}
