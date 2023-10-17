using Jordnaer.Chat;
using Jordnaer.Shared.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddAzureAppConfiguration("appconfig");

    builder.AddSerilog();

    builder.AddDatabase();

    builder.AddMassTransit();

    var app = builder.Build();

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    app.UseSerilog();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseAzureAppConfiguration();

    app.MapHealthChecks("/health").AllowAnonymous();

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "An unhandled exception occurred during application startup.");
}
finally
{
    Log.CloseAndFlush();
}

namespace Jordnaer.Chat
{
    public partial class Program { }
}
