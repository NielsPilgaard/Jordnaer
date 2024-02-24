using Azure.Storage.Blobs;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using Blazr.RenderState.Server;
using Jordnaer.Components;
using Jordnaer.Components.Account;
using Jordnaer.Database;
using Jordnaer.Extensions;
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
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using MudBlazor;
using MudBlazor.Services;
using MudExtensions.Services;
using Serilog;
using System.Text.Json.Serialization;
using Jordnaer.Features.Authentication;

Log.Logger = new LoggerConfiguration()
			 .WriteTo.Console()
			 .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
	   .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddCurrentUser();

builder.Services.AddAuthentication(options =>
{
	options.DefaultScheme = IdentityConstants.ApplicationScheme;
	options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
}).AddFacebook(options =>
{
	options.AppId = builder.Configuration.GetValue<string>("Authentication:Schemes:Facebook:AppId")!;
	options.AppSecret = builder.Configuration.GetValue<string>("Authentication:Schemes:Facebook:AppSecret")!;
	options.SaveTokens = true;
}).AddMicrosoftAccount(options =>
{
	options.ClientId = builder.Configuration.GetValue<string>("Authentication:Schemes:Microsoft:ClientId")!;
	options.ClientSecret = builder.Configuration.GetValue<string>("Authentication:Schemes:Microsoft:ClientSecret")!;
	options.SaveTokens = true;
}).AddGoogle(options =>
{
	options.ClientId = builder.Configuration.GetValue<string>("Authentication:Schemes:Google:ClientId")!;
	options.ClientSecret = builder.Configuration.GetValue<string>("Authentication:Schemes:Google:ClientSecret")!;
	options.SaveTokens = true;
}).AddIdentityCookies();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
	options.SignIn.RequireConfirmedAccount = true;
	options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<JordnaerDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

builder.Services.AddAuthorization();

builder.AddAzureAppConfiguration();

builder.AddSerilog();

builder.AddDatabase();

builder.Services.AddRateLimiting();

builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

builder.Services.AddHttpClient(HttpClients.Default)
				.AddStandardResilienceHandler();

builder.Services.AddUserSearchFeature();

builder.Services.ConfigureHttpJsonOptions(options =>
	options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.AddEmailServices();

builder.AddDeleteUserFeature();

builder.Services.AddSingleton(_ =>
	new BlobServiceClient(builder.Configuration.GetConnectionString("AzureBlobStorage")));

builder.AddMassTransit();

builder.AddSignalR();

builder.Services.AddScoped<IImageService, ImageService>();

builder.AddGroupServices();
builder.AddGroupSearchServices();

builder.AddBlazrRenderStateServerServices();

builder.AddCategoryServices();
builder.AddProfileServices();
builder.AddChatServices();

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

app.UseStaticFiles();
app.UseRouting();

app.UseSerilog();

app.UseAzureAppConfiguration();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
	ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

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
	public class Program;
}
