using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Jordnaer.Client.Features.Profile;

public static class WebAssemblyHostBuilderExtensions
{
    public static WebAssemblyHostBuilder AddProfileServices(this WebAssemblyHostBuilder builder)
    {
        builder.Services.AddRefitClient<IProfileClient>(builder.HostEnvironment.BaseAddress);
        builder.Services.AddSingleton<IProfileCache, ProfileCache>();

        return builder;
    }
}
