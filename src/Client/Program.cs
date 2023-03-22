using Jordnaer.Client;
using Jordnaer.Client.Authentication;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient<AuthClient>(client =>
    client.BaseAddress = new Uri($"{builder.HostEnvironment.BaseAddress}api/"));

builder.Services.AddHttpClient<UserClient>(client =>
    client.BaseAddress = new Uri($"{builder.HostEnvironment.BaseAddress}api/"));

builder.Services.AddMudServices();

builder.Services.AddWasmAuthentication();

await builder.Build().RunAsync();
