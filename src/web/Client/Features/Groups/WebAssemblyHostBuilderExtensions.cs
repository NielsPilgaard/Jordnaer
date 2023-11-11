using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Refit;

namespace Jordnaer.Client.Features.Groups;

public static class WebAssemblyHostBuilderExtensions
{
    public static WebAssemblyHostBuilder AddGroupServices(this WebAssemblyHostBuilder builder)
    {
        builder.Services.AddRefitClient<IGroupClient>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
        builder.Services.AddRefitClient<IGroupChatClient>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
        return builder;
    }
}
