using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Jordnaer.Client.Features.LookingFor;

public static class WebAssemblyHostBuilderExtensions
{
    public static WebAssemblyHostBuilder AddLookingForServices(this WebAssemblyHostBuilder builder)
    {
        builder.Services.AddRefitClient<ILookingForApiClient>(builder.HostEnvironment.BaseAddress);
        builder.Services.AddSingleton<ILookingForCache, LookingForCache>();

        return builder;
    }
}
