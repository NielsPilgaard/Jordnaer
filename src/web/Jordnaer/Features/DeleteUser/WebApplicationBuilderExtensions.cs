namespace Jordnaer.Server.Features.DeleteUser;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddDeleteUserFeature(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IDeleteUserService, DeleteUserService>();

        return builder;
    }
}
