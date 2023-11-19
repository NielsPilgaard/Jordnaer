using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Refit;

namespace Jordnaer.Client.Features.GroupSearch;

public static class WebAssemblyHostBuilderExtensions
{
    public static WebAssemblyHostBuilder AddGroupSearchServices(this WebAssemblyHostBuilder builder)
    {
        builder.Services.AddRefitClient<IGroupSearchClient>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
        return builder;
    }
}
