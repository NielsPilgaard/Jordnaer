using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using RemindMeApp.Server.Data;

namespace RemindMeApp.Server.Authentication;

public static class AuthExtensions
{
    private delegate void ExternalAuthProvider(AuthenticationBuilder authenticationBuilder, Action<object> configure);

    public static WebApplicationBuilder AddAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddEntityFrameworkStores<RemindMeDbContext>();

        // Used to send email confirmation links, reset password etc
        builder.Services.AddTransient<IEmailSender, EmailSender>();

        // Our default scheme is cookies
        var authenticationBuilder = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);

        // Add the default authentication cookie that will be used between the front end and
        // the backend.
        authenticationBuilder.AddCookie();

        // This is the cookie that will store the user information from the external login provider
        authenticationBuilder.AddCookie(AuthConstants.ExternalScheme);

        // Add external auth providers based on configuration
        //{
        //    "Authentication": {
        //        "Schemes": {
        //            "<scheme>": {
        //                "ClientId": "xxx",
        //                "ClientSecret": "xxxx"
        //                etc..
        //            }
        //        }
        //    }
        //}

        // These are the list of external providers available to the application.
        // Many more are available from https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers
        var externalProviders = new Dictionary<string, ExternalAuthProvider>
        {
            ["Facebook"] = static (builder, configure) => builder.AddFacebook(configure),
            ["Google"] = static (builder, configure) => builder.AddGoogle(configure),
            ["Microsoft"] = static (builder, configure) => builder.AddMicrosoftAccount(configure),
            ["GitHub"] = static (builder, configure) => builder.AddGitHub(configure),
        };

        foreach (var (providerName, provider) in externalProviders)
        {
            var section = builder.Configuration.GetSection($"Authentication:Schemes:{providerName}");

            if (section.Exists())
            {
                provider(authenticationBuilder, options =>
                {
                    // Bind this section to the specified options
                    section.Bind(options);

                    // This will save the information in the external cookie
                    if (options is RemoteAuthenticationOptions remoteAuthenticationOptions)
                    {
                        remoteAuthenticationOptions.SignInScheme = AuthConstants.ExternalScheme;
                    }
                });
            }
        }

        // Allows the UI to resolve external providers
        builder.Services.AddSingleton<ExternalProviders>();

        builder.Services.AddScoped<IUserService, UserService>();

        return builder;
    }

    private static readonly string ExternalProviderKey = "ExternalProviderName";
    private static readonly string HasExternalTokenKey = "ExternalToken";

    internal static string GetUserId(this HttpContext context) => context.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    internal static bool UserIsAdmin(this HttpContext context) => context.User.IsInRole("admin");

    public static string? GetExternalProvider(this AuthenticationProperties properties) =>
        properties.GetString(ExternalProviderKey);

    public static void SetExternalProvider(this AuthenticationProperties properties, string providerName) =>
        properties.SetString(ExternalProviderKey, providerName);

    public static bool HasExternalToken(this AuthenticationProperties properties) =>
        properties.GetString(HasExternalTokenKey) is not null;

    public static void SetHasExternalToken(this AuthenticationProperties properties, bool hasToken)
    {
        if (hasToken)
        {
            properties.SetString(HasExternalTokenKey, "1");
        }
        else
        {
            properties.Items.Remove(HasExternalTokenKey);
        }
    }
}
