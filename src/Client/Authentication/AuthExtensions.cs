using Microsoft.AspNetCore.Components.Authorization;

namespace RemindMeApp.Client.Authentication;

public static class AuthExtensions
{
    public static IServiceCollection AddWasmAuthentication(this IServiceCollection services)
    {
        services.AddOptions();
        services.AddAuthorizationCore();
        services.AddScoped<AuthStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<AuthStateProvider>());
        return services;
    }
}
