using Jordnaer.Server.Database;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Jordnaer.Server.Authentication;

public static class AuthExtensions
{
    private delegate void ExternalAuthProvider(AuthenticationBuilder authenticationBuilder, Action<object> configure);

    public static WebApplicationBuilder AddAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<JordnaerDbContext>();

        // Used to send email confirmation links, reset password etc
        builder.Services.AddTransient<IEmailSender, EmailSender>();

        // Our default scheme is cookies
        var authenticationBuilder = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);

        // Add the default authentication cookie that will be used between the front end and
        // the backend.
        authenticationBuilder.AddCookie();

        // This is the cookie that will store the user information from the external login provider
        authenticationBuilder.AddCookie(AuthConstants.ExternalScheme);

        // These are the list of external providers available to the application.
        // Many more are available from https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers
        var externalProviders = new Dictionary<string, ExternalAuthProvider>
        {
            ["Facebook"] = static (builder, configure) => builder.AddFacebook(configure),
            ["Google"] = static (builder, configure) => builder.AddGoogle(configure),
            ["Microsoft"] = static (builder, configure) => builder.AddMicrosoftAccount(configure)
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

        builder.Services.AddScoped<IUserService, UserService>();

        return builder;
    }

    private const string EXTERNAL_PROVIDER_KEY = "ExternalProviderName";

    public static string? GetExternalProvider(this AuthenticationProperties properties) =>
        properties.GetString(EXTERNAL_PROVIDER_KEY);

    public static void SetExternalProvider(this AuthenticationProperties properties, string providerName) =>
        properties.SetString(EXTERNAL_PROVIDER_KEY, providerName);
}
