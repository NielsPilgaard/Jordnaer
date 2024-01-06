using Azure.Storage.Blobs;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using Blazr.RenderState.Server;
using Jordnaer.Authentication;
using Jordnaer.Authorization;
using Jordnaer.Components;
using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Features.Authentication;
using Jordnaer.Features.Category;
using Jordnaer.Features.Chat;
using Jordnaer.Features.DeleteUser;
using Jordnaer.Features.Email;
using Jordnaer.Features.Groups;
using Jordnaer.Features.GroupSearch;
using Jordnaer.Features.Profile;
using Jordnaer.Features.UserSearch;
using Jordnaer.Shared;
using Jordnaer.Shared.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor;
using MudBlazor.Services;
using MudExtensions.Services;
using Serilog;
using System.Text.Json.Serialization;

Log.Logger = new LoggerConfiguration()
             .WriteTo.Console()
             .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

var baseUrl = builder.Configuration.GetValue<string>("MiniMoeder:BaseUrl")
                 ?? throw new ArgumentNullException("BaseUrl", "BaseUrl must be set in the configuration.");

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

builder.AddEmailServices(baseUrl);

builder.AddDeleteUserFeature();

builder.Services.AddSingleton(_ =>
    new BlobServiceClient(builder.Configuration.GetConnectionString("AzureBlobStorage")));

builder.Services.AddHttpContextAccessor();

builder.AddMassTransit();

builder.AddSignalR();

builder.Services.AddScoped<IImageService, ImageService>();

builder.AddGroupServices(baseUrl);
builder.AddGroupSearchServices(baseUrl);

builder.AddBlazrRenderStateServerServices();

builder.AddCategoryServices(baseUrl);
builder.AddProfileServices(baseUrl);
builder.Services.AddRefitClient<IAuthClient>(baseUrl);
builder.Services.AddRefitClient<IDeleteUserClient>(baseUrl);
builder.Services.AddRefitClient<IUserSearchClient>(baseUrl);
builder.Services.AddRefitClient<IImageClient>(baseUrl);
builder.Services.AddRefitClient<IChatClient>(baseUrl);

//TODO: Remove
builder.Services.AddWasmAuthentication();

builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<ChatSignalRClient>();

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

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredSessionStorage();

builder.Services.AddMemoryCache();

builder.Services.AddDataForsyningenClient();
builder.Services.Configure<DataForsyningenOptions>(builder.Configuration.GetSection(DataForsyningenOptions.SectionName));

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
    //.AddAdditionalAssemblies(typeof(Routes).Assembly)
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

namespace Jordnaer
{
    public partial class Program { }
}
