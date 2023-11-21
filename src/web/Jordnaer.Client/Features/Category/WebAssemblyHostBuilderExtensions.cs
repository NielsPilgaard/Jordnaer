using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Jordnaer.Client.Features.Category;

public static class WebAssemblyHostBuilderExtensions
{
    public static WebAssemblyHostBuilder AddCategoryServices(this WebAssemblyHostBuilder builder)
    {
        builder.Services.AddRefitClient<ICategoryClient>(builder.HostEnvironment.BaseAddress);
        builder.Services.AddSingleton<ICategoryCache, CategoryCache>();

        return builder;
    }
}
