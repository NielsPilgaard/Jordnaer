using Microsoft.AspNetCore.Components.Authorization;

namespace Jordnaer.Client.Features.Authentication;

public static class AuthExtensions
{
    public static IServiceCollection AddWasmAuthentication(this IServiceCollection services)
    {
        services.AddOptions();
        services.AddAuthorizationCore();
        services.AddScoped<AuthStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<AuthStateProvider>());
        services.AddCascadingAuthenticationState();
        return services;
    }
}
