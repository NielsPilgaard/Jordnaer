using Jordnaer.Server.Authentication;
using Jordnaer.Server.Authorization;
using Jordnaer.Server.Database;
using Jordnaer.Server.Extensions;
using Jordnaer.Server.Features.Profile;
using Microsoft.FeatureManagement;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddAzureAppConfiguration();

    builder.Services.AddApplicationInsightsTelemetry();
    builder.AddSerilog();

    string dbConnectionString = builder.Configuration.GetConnectionString(nameof(JordnaerDbContext)) ??
                              throw new InvalidOperationException(
                                  $"Connection string '{nameof(JordnaerDbContext)}' not found.");
    builder.Services.AddSqlServer<JordnaerDbContext>(dbConnectionString);
    builder.Services.AddHealthChecks().AddSqlServer(dbConnectionString);

    builder.Services.AddCurrentUser();

    builder.AddAuthentication();
    builder.Services.AddAuthorizationBuilder().AddCurrentUserHandler();

    builder.Services.AddRateLimiting();

    builder.Services.AddFeatureManagement();

    builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

    builder.Services.AddResilientHttpClient();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseWebAssemblyDebugging();
    }
    else
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseAzureAppConfiguration();

    app.UseRateLimiter();

    app.UseHttpsRedirection();

    app.UseBlazorFrameworkFiles();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    // Configure the APIs
    app.MapAuthentication();
    app.MapUsers();
    app.MapProfiles();

    app.MapHealthChecks("/health").AllowAnonymous().RequireHealthCheckRateLimit();

    app.MapFallbackToFile("index.html");

    await app.InitializeDatabaseAsync();

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

public partial class Program { }
