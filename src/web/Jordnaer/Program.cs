using System.Text.Json.Serialization;
using Azure.Storage.Blobs;
using Jordnaer.Server;
using Jordnaer.Server.Authentication;
using Jordnaer.Server.Authorization;
using Jordnaer.Server.Database;
using Jordnaer.Server.Extensions;
using Jordnaer.Server.Features.Category;
using Jordnaer.Server.Features.Chat;
using Jordnaer.Server.Features.DeleteUser;
using Jordnaer.Server.Features.Email;
using Jordnaer.Server.Features.Groups;
using Jordnaer.Server.Features.GroupSearch;
using Jordnaer.Server.Features.Profile;
using Jordnaer.Server.Features.UserSearch;
using Jordnaer.Shared.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.FeatureManagement;
using Serilog;
using _Imports = Jordnaer.Client._Imports;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddRazorComponents()
        .AddInteractiveWebAssemblyComponents();

    builder.AddAzureAppConfiguration();

    builder.AddSerilog();

    builder.Services.AddProblemDetails();

    builder.AddDatabase();

    builder.Services.AddCurrentUser();

    builder.AddAuthentication();
    builder.Services.AddAuthorizationBuilder().AddCurrentUserHandler();

    builder.Services.AddRateLimiting();

    builder.Services.AddFeatureManagement();

    builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

    builder.Services.AddResilientHttpClient();

    builder.Services.AddUserSearchFeature();

    builder.Services.AddOutputCache();

    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    builder.AddEmailServices();

    builder.AddDeleteUserFeature();

    builder.Services.AddSingleton(_ =>
        new BlobServiceClient(builder.Configuration.GetConnectionString("AzureBlobStorage")));

    builder.Services.AddHttpContextAccessor();

    builder.AddMassTransit();

    builder.AddAzureSignalR();

    builder.Services.AddScoped<IImageService, ImageService>();

    builder.AddGroupServices();
    builder.AddGroupSearchServices();

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseResponseCompression();
    }

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    app.UseSerilog();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseWebAssemblyDebugging();
        await app.InitializeDatabaseAsync();
    }
    else
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseAzureAppConfiguration();

    app.UseHttpsRedirection();

    app.UseStaticFiles();

    app.UseRouting();
    app.UseAntiforgery();
    app.UseRateLimiter();
    app.UseOutputCache();

    app.UseAuthentication();
    app.UseAuthorization();

    // Configure the APIs
    app.MapAuthentication();
    app.MapProfiles();
    app.MapCategories();
    app.MapUserSearch();
    app.MapEmail();
    app.MapImages();
    app.MapDeleteUsers();
    app.MapChat();
    app.MapGroups();
    app.MapGroupSearch();

    app.MapHealthChecks("/health").AllowAnonymous().RequireHealthCheckRateLimit();

    app.MapHub<ChatHub>("/hubs/chat");

    app.MapRazorComponents<App>()
        .AddInteractiveWebAssemblyRenderMode()
        .AddAdditionalAssemblies(typeof(_Imports).Assembly);

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "An unhandled exception occurred.");
}
finally
{
    // Wait 0.5 seconds before closing and flushing, to gather the last few logs.
    await Task.Delay(TimeSpan.FromMilliseconds(500));
    await Log.CloseAndFlushAsync();
}

public partial class Program { }
