﻿using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jordnaer.Features.Authentication;

public static class CircuitHandlerExtensions
{
	public static IServiceCollection AddCurrentUser(this IServiceCollection services)
	{
		services.AddScoped<CurrentUser>();
		services.AddScoped<CookieFactory>();
		services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, UserCircuitHandler>());
		return services;
	}
}