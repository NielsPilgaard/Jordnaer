using Azure.Storage.Blobs;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
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
using Jordnaer.Features.Images;
using Jordnaer.Features.Membership;
using Jordnaer.Features.GroupPosts;
using Jordnaer.Features.Posts;
using Jordnaer.Features.PostSearch;
using Jordnaer.Features.Map;
using Jordnaer.Features.Profile;
using Jordnaer.Features.Search;
using Jordnaer.Features.UserSearch;
using Jordnaer.Shared;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.FeatureManagement;
using Serilog;
using Sidio.Sitemap.AspNetCore;
using Sidio.Sitemap.Blazor;
using Sidio.Sitemap.Core.Services;
using System.Text.Json.Serialization;

Log.Logger = new LoggerConfiguration()
			 .WriteTo.Console()
			 .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
	   .AddInteractiveServerComponents();

builder.Services.AddFeatureManagement();

builder.AddAuthentication();

builder.Services.AddAuthorization();

builder.AddSerilog();

builder.AddDatabase();

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

builder.AddSearchServices();
builder.AddGroupServices();
builder.AddGroupSearchServices();

builder.AddCategoryServices();
builder.AddProfileServices();
builder.AddChatServices();
builder.AddMembershipServices();
builder.AddPostFeature();
builder.AddGroupPostFeature();
builder.AddPostSearchFeature();

builder.AddMudBlazor();

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredSessionStorage();

builder.Services.AddMemoryCache();

builder.Services.AddDataForsyningenClient();

builder.Services.AddScoped<ILeafletMapInterop, LeafletMapInterop>();

builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

builder.AddOpenTelemetry();

builder.Services.AddScoped<UserSearchResultCache>();
builder.Services.AddScoped<GroupSearchResultCache>();

builder.Services
	   .AddHttpContextAccessor()
	   .AddDefaultSitemapServices<HttpContextBaseUrlProvider>();

builder.Services.AddRazorPages();

var app = builder.Build();

app.UseSecurityHeaders(policies => policies.AddFrameOptionsDeny()
										   .AddXssProtectionBlock()
										   .AddContentTypeOptionsNoSniff()
										   .AddStrictTransportSecurityMaxAge()
										   .AddReferrerPolicyStrictOriginWhenCrossOrigin()
										   .RemoveServerHeader());

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
	app.UseHttpsRedirection();
}

app.MapStaticAssets();

app.UseRouting();

app.UseSerilog();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorPages();

app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

app.MapObservabilityEndpoints();

app.MapHub<ChatHub>("/hubs/chat");

app.UseSitemap();

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
	await Log.CloseAndFlushAsync();
}

namespace Jordnaer
{
	public class Program;
}
