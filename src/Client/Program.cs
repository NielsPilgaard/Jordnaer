using Blazored.LocalStorage;
using Jordnaer.Client;
using Jordnaer.Client.Authentication;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FeatureManagement;
using MudBlazor;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient<AuthClient>(client =>
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

builder.Services.AddHttpClient<UserClient>(client =>
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

builder.AddResilientHttpClient();

builder.Services.AddMudServices(configuration => configuration.SnackbarConfiguration = new SnackbarConfiguration
{
    VisibleStateDuration = 2500,
    ShowTransitionDuration = 250,
    BackgroundBlurred = true,
    MaximumOpacity = 95,
    MaxDisplayedSnackbars = 3,
    PositionClass = Defaults.Classes.Position.BottomCenter,
    HideTransitionDuration = 100,
    ShowCloseIcon = false
});

builder.Services.AddWasmAuthentication();

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddMemoryCache();

await builder.Build().RunAsync();
