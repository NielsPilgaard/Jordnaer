using Azure.Storage.Blobs;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using Jordnaer.Components.Account;
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
using Serilog;
using System.Text.Json.Serialization;
using Jordnaer.Components;
using Jordnaer.Features.Images;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Jordnaer.Database;
using Jordnaer.Features.Membership;
using Jordnaer.Features.Search;
using Microsoft.FeatureManagement;
using Microsoft.Net.Http.Headers;
using Sidio.Sitemap.AspNetCore;
using Sidio.Sitemap.Blazor;
using Sidio.Sitemap.Core.Services;

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

builder.AddMudBlazor();

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredSessionStorage();

builder.Services.AddMemoryCache();

builder.Services.AddDataForsyningenClient();

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
}

app.UseHttpsRedirection();

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
	// Wait 0.5 seconds before closing and flushing, to gather the last few logs.
	await Task.Delay(TimeSpan.FromMilliseconds(500));
	await Log.CloseAndFlushAsync();
}

namespace Jordnaer
{
	public class Program;
}
