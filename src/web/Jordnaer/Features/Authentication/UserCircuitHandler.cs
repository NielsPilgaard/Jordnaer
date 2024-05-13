using System.Net;
using Jordnaer.Extensions;
using Jordnaer.Features.Profile;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Jordnaer.Features.Authentication;

internal sealed class UserCircuitHandler(
	AuthenticationStateProvider authenticationStateProvider,
	CurrentUser currentUser,
	IProfileCache profileCache,
	ILogger<UserCircuitHandler> logger,
	IHttpContextAccessor httpContextAccessor,
	CookieFactory cookieFactory)
	: CircuitHandler, IDisposable
{
	public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
	{
		authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationChanged;
		profileCache.ProfileChanged += OnProfileChanged;

		var state = await authenticationStateProvider.GetAuthenticationStateAsync();
		currentUser.User = state.User;
		currentUser.UserProfile = await profileCache.GetProfileAsync(cancellationToken);

		await base.OnCircuitOpenedAsync(circuit, cancellationToken);
	}

	private void OnProfileChanged(object? sender, UserProfile userProfile)
		=> currentUser.UserProfile = userProfile;

	private void OnAuthenticationChanged(Task<AuthenticationState> authenticationChanged)
	{
		_ = UpdateAuthentication(authenticationChanged);

		return;

		async Task UpdateAuthentication(Task<AuthenticationState> task)
		{
			try
			{
				var state = await task;
				currentUser.User = state.User;
			}
			catch (Exception exception)
			{
				logger.LogException(exception);
			}
		}
	}

	public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
	{
		if (currentUser.CookieContainer?.Count is > 3)
		{
			logger.LogTrace("CurrentUser already has a Cookie Container with 3 or more cookies, returning.");
			return Task.CompletedTask;
		}

		if (currentUser.Id is null)
		{
			logger.LogTrace("CurrentUser is not logged in yet, returning.");
			return Task.CompletedTask;
		}

		if (httpContextAccessor.HttpContext is null)
		{
			logger.LogWarning("No HttpContext is associated with Circuit {CircuitId}", circuit.Id);
			return Task.CompletedTask;
		}

		var domain = httpContextAccessor.HttpContext.Request.Host.Host;

		currentUser.CookieContainer = new CookieContainer();

		string[] cookieNames = ["ARRAffinity", "ARRAffinitySameSite", AuthenticationConstants.CookieName];

		foreach (var cookieName in cookieNames)
		{
			AddCookieToCookieContainer(cookieName, domain);
		}

		logger.LogDebug("Finished setting cookies for User {UserId}", currentUser.Id);

		return Task.CompletedTask;
	}

	private void AddCookieToCookieContainer(string cookieName, string domain)
	{
		if (httpContextAccessor.HttpContext!.Request.Cookies.TryGetValue(cookieName, out var sessionAffinityCookie))
		{
			currentUser.CookieContainer!.Add(
				cookieFactory.Create(name: cookieName,
									 cookie: sessionAffinityCookie,
									 // Session Affinity Cookies have a "." as prefix
									 domain: cookieName.StartsWith("ARR") ? $".{domain}" : domain));
		}
		else
		{
			logger.LogWarning("Failed to get cookie by name '{CookieName}' " +
							  "for logged in User {UserId}. SignalR Connection may be in a broken state.",
							  cookieName, currentUser.Id);
		}
	}

	public void Dispose()
	{
		authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationChanged;
		profileCache.ProfileChanged -= OnProfileChanged;
	}
}