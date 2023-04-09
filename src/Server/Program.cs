using Jordnaer.Server.Authentication;
using Jordnaer.Server.Authorization;
using Jordnaer.Server.Data;
using Jordnaer.Server.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddSerilog();

    string connectionString = builder.Configuration.GetConnectionString(nameof(JordnaerDbContext)) ??
                              throw new InvalidOperationException(
                                  $"Connection string '{nameof(JordnaerDbContext)}' not found.");
    builder.Services.AddSqlite<JordnaerDbContext>(connectionString);

    builder.AddAuthentication();
    builder.Services.AddAuthorizationBuilder().AddCurrentUserHandler();

    // State that represents the current user from the database *and* the request
    builder.Services.AddCurrentUser();

    // Configure rate limiting
    builder.Services.AddRateLimiting();

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

    app.MapFallbackToFile("index.html");

    //await app.InitializeDatabaseAsync();

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
