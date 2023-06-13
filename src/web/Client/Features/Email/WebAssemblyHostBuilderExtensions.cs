using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Jordnaer.Client.Features.Email;

public static class WebAssemblyHostBuilderExtensions
{
    public static WebAssemblyHostBuilder AddEmailServices(this WebAssemblyHostBuilder builder)
    {
        builder.Services.AddRefitClient<IEmailApiClient>(builder.HostEnvironment.BaseAddress);

        return builder;
    }
}
