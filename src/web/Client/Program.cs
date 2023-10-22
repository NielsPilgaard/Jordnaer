using Blazored.LocalStorage;
using Blazored.SessionStorage;
using Jordnaer.Client;
using Jordnaer.Client.Features.Authentication;
using Jordnaer.Client.Features.Category;
using Jordnaer.Client.Features.Chat;
using Jordnaer.Client.Features.Email;
using Jordnaer.Client.Features.Profile;
using Jordnaer.Client.Features.UserSearch;
using Jordnaer.Shared;
using MassTransit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using MudExtensions.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddRefitClient<IAuthClient>(builder.HostEnvironment.BaseAddress);
builder.Services.AddRefitClient<IDeleteUserClient>(builder.HostEnvironment.BaseAddress);
builder.Services.AddRefitClient<IUserSearchClient>(builder.HostEnvironment.BaseAddress);
builder.Services.AddRefitClient<IImageClient>(builder.HostEnvironment.BaseAddress);
builder.Services.AddRefitClient<IChatClient>(builder.HostEnvironment.BaseAddress);

builder.AddCategoryServices();

builder.AddProfileServices();

builder.AddEmailServices();

// Allows using NewId in Blazor WASM
NewId.SetProcessIdProvider(null);

builder.Services.AddMudServices(configuration =>
{
    configuration.ResizeOptions = new ResizeOptions
    {
        NotifyOnBreakpointOnly = true
    };
    configuration.SnackbarConfiguration = new SnackbarConfiguration
    {
        VisibleStateDuration = 2500,
        ShowTransitionDuration = 250,
        BackgroundBlurred = true,
        MaximumOpacity = 95,
        MaxDisplayedSnackbars = 3,
        PositionClass = Defaults.Classes.Position.BottomCenter,
        HideTransitionDuration = 100,
        ShowCloseIcon = false
    };
});
builder.Services.AddMudExtensions();

builder.Services.AddWasmAuthentication();

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredSessionStorage();

builder.Services.AddMemoryCache();

builder.Services.AddDataForsyningenClient();
builder.Services.Configure<DataForsyningenOptions>(
    builder.Configuration.GetSection(DataForsyningenOptions.SectionName));

builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<ChatSignalRClient>();

var host = builder.Build();

await host.RunAsync();
