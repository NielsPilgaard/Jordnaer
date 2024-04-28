using Jordnaer.Components.Account;
using Jordnaer.Database;
using Jordnaer.Features.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Serilog;

namespace Jordnaer.Extensions;

public static class AuthenticationExtensions
{
	public static WebApplicationBuilder AddAuthentication(this WebApplicationBuilder builder)
	{
		builder.Services.AddCascadingAuthenticationState();
		builder.Services.AddScoped<IdentityUserAccessor>();
		builder.Services.AddScoped<IdentityRedirectManager>();
		builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
		builder.Services.AddCurrentUser();

		builder.Services
			   .AddAuthentication(options =>
			   {
				   options.DefaultScheme = IdentityConstants.ApplicationScheme;
				   options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
			   })
			   .AddFacebookAuthentication(builder.Configuration, builder.Environment.IsDevelopment())
			   .AddMicrosoftAuthentication(builder.Configuration, builder.Environment.IsDevelopment())
			   .AddGoogleAuthentication(builder.Configuration, builder.Environment.IsDevelopment())
			   .AddIdentityCookies();

		builder.Services
			   .AddIdentityCore<ApplicationUser>(options =>
			   {
				   options.SignIn.RequireConfirmedAccount = true;
				   options.User.RequireUniqueEmail = true;
			   })
			   .AddEntityFrameworkStores<JordnaerDbContext>()
			   .AddSignInManager()
			   .AddDefaultTokenProviders();

		return builder;
	}

	private static AuthenticationBuilder AddGoogleAuthentication(
		this AuthenticationBuilder builder,
		ConfigurationManager configuration, bool isDevelopment)
	{
		var googleOptions = new GoogleOptions();
		configuration.GetSection("Authentication:Schemes:Google").Bind(googleOptions);

		if ((string.IsNullOrEmpty(googleOptions.ClientId) || string.IsNullOrEmpty(googleOptions.ClientSecret))
			&& isDevelopment)
		{
			Log.Warning("Google Authentication is not enabled, " +
						"because it's configuration section was not set. " +
						"SectionName {SectionName}", "Authentication:Schemes:Google");
			return builder;
		}

		builder.AddGoogle(options =>
		{
			options.ClientId = googleOptions.ClientId;
			options.ClientSecret = googleOptions.ClientSecret;
			options.SaveTokens = true;
		});

		return builder;
	}

	private static AuthenticationBuilder AddFacebookAuthentication(
		this AuthenticationBuilder builder,
		ConfigurationManager configuration,
		bool isDevelopment)
	{
		var facebookOptions = new FacebookOptions();
		configuration.GetSection("Authentication:Schemes:Facebook").Bind(facebookOptions);

		if ((string.IsNullOrEmpty(facebookOptions.AppId) || string.IsNullOrEmpty(facebookOptions.AppSecret))
			&& isDevelopment)
		{
			Log.Warning("Google Authentication is not enabled, " +
							"because it's configuration section was not set. " +
							"SectionName {SectionName}", "Authentication:Schemes:Google");
			return builder;
		}

		builder.AddFacebook(options =>
		{
			options.AppId = facebookOptions.AppId;
			options.AppSecret = facebookOptions.AppSecret;
			options.SaveTokens = true;
		});

		return builder;
	}

	private static AuthenticationBuilder AddMicrosoftAuthentication(
		this AuthenticationBuilder builder,
		ConfigurationManager configuration,
		bool isDevelopment)
	{
		var microsoftOptions = new MicrosoftAccountOptions();
		configuration.GetSection("Authentication:Schemes:Microsoft").Bind(microsoftOptions);

		if ((string.IsNullOrEmpty(microsoftOptions.ClientId) || string.IsNullOrEmpty(microsoftOptions.ClientSecret))
			&& isDevelopment)
		{
			Log.Warning("Google Authentication is not enabled, " +
						"because it's configuration section was not set. " +
						"SectionName {SectionName}", "Authentication:Schemes:Google");
			return builder;
		}

		builder.AddMicrosoftAccount(options =>
		{
			options.ClientId = microsoftOptions.ClientId;
			options.ClientSecret = microsoftOptions.ClientSecret;
			options.SaveTokens = true;
		});

		return builder;
	}
}
