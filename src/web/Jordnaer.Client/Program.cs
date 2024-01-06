using Blazr.RenderState.WASM;
using MassTransit;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Allows using NewId in Blazor WASM
NewId.SetProcessIdProvider(null);

builder.AddBlazrRenderStateWASMServices();

var host = builder.Build();

await host.RunAsync();
