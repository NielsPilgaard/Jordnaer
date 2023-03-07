using RemindMeApp.Server.Authentication;
using RemindMeApp.Server.Authorization;
using RemindMeApp.Server.Data;
using RemindMeApp.Server.Extensions;
using RemindMeApp.Server.Reminders;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilog();

string connectionString = builder.Configuration.GetConnectionString("RemindMeDbContext") ??
                          throw new InvalidOperationException("Connection string 'RemindMeDbContext' not found.");
builder.Services.AddSqlite<RemindMeDbContext>(connectionString);

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
app.MapReminders();

app.MapFallbackToFile("index.html");

await app.InitializeDatabaseAsync();

app.Run();
