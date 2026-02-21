namespace Jordnaer.Features.HjemGroups;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddHjemGroupServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient<HjemGroupScraperService>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (compatible; MiniMoeder/1.0)");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        builder.Services.AddScoped<IHjemGroupProvider, HjemGroupProvider>();
        builder.Services.AddHostedService<HjemGroupScraperBackgroundService>();

        return builder;
    }
}
