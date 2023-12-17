using System.Text.Json.Serialization;
using Azure.Storage.Blobs;
using Jordnaer.Client;
using Jordnaer.Server.Authentication;
using Jordnaer.Server.Authorization;
using Jordnaer.Server.Components;
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

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddInteractiveServerComponents();

builder.AddAzureAppConfiguration();

builder.AddSerilog();

builder.AddDatabase();

builder.Services.AddCurrentUser();

builder.AddAuthentication();
builder.Services.AddAuthorizationBuilder().AddCurrentUserHandler();

builder.Services.AddRateLimiting();

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

builder.AddSignalR();

builder.Services.AddScoped<IImageService, ImageService>();

builder.AddGroupServices();
builder.AddGroupSearchServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    await app.InitializeDatabaseAsync();
}
else
{
    app.UseResponseCompression();
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

app.UseSerilog();

app.UseAzureAppConfiguration();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseRateLimiter();
app.UseOutputCache();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly)
    .AddInteractiveServerRenderMode();

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

try
{
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
