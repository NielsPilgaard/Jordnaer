using System.Security.Claims;
using System.Threading.RateLimiting;

namespace Jordnaer.Extensions;

public static class RateLimitExtensions
{
	private const string PerUserRatelimitPolicy = "PerUserRateLimit";
	private const string HealthCheckRatelimitPolicy = "HealthCheckRateLimit";
	private const string AuthRatelimitPolicy = "AuthenticationRateLimit";
	private const string SearchRatelimitPolicy = "UserSearchRateLimit";

	private const string AnonymousPartition = "Anonymous";

	public static IServiceCollection AddRateLimiting(this IServiceCollection services) =>
		services.AddRateLimiter(options =>
		{
			options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

			options.AddPolicy(PerUserRatelimitPolicy, context =>
			{
				var username = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

				return username is not null
					? RateLimitPartition.GetFixedWindowLimiter(username, _ => new FixedWindowRateLimiterOptions
					{
						Window = TimeSpan.FromSeconds(15),
						PermitLimit = 50
					})
					: RateLimitPartition.GetFixedWindowLimiter(AnonymousPartition, _ => new FixedWindowRateLimiterOptions
					{
						Window = TimeSpan.FromSeconds(1),
						PermitLimit = 50
					});
			});

			options.AddPolicy(HealthCheckRatelimitPolicy, _ =>
			{
				return RateLimitPartition.GetFixedWindowLimiter(HealthCheckRatelimitPolicy, _ => new FixedWindowRateLimiterOptions
				{
					// 5 messages per 10 seconds
					Window = TimeSpan.FromSeconds(10),
					PermitLimit = 5
				});
			});

			options.AddPolicy(AuthRatelimitPolicy, context =>
			{
				var username = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

				return username is null
					? RateLimitPartition.GetFixedWindowLimiter(AnonymousPartition,
						_ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromSeconds(1), PermitLimit = 50 })
					: RateLimitPartition.GetNoLimiter(username);
			});

			options.AddPolicy(SearchRatelimitPolicy,
				context =>
				{
					var username = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

					return username is not null
						? RateLimitPartition.GetFixedWindowLimiter(username, _ => new FixedWindowRateLimiterOptions
						{
							Window = TimeSpan.FromSeconds(10),
							PermitLimit = 30
						})
						: RateLimitPartition.GetFixedWindowLimiter(AnonymousPartition, _ => new FixedWindowRateLimiterOptions
						{
							Window = TimeSpan.FromSeconds(1),
							PermitLimit = 15
						});
				});
		});

	public static IEndpointConventionBuilder RequirePerUserRateLimit(this IEndpointConventionBuilder builder) => builder.RequireRateLimiting(PerUserRatelimitPolicy);
	public static IEndpointConventionBuilder RequireHealthCheckRateLimit(this IEndpointConventionBuilder builder) => builder.RequireRateLimiting(HealthCheckRatelimitPolicy);
	public static IEndpointConventionBuilder RequireAuthRateLimit(this IEndpointConventionBuilder builder) => builder.RequireRateLimiting(AuthRatelimitPolicy);
	public static IEndpointConventionBuilder RequireSearchRateLimit(this IEndpointConventionBuilder builder) => builder.RequireRateLimiting(SearchRatelimitPolicy);
}
