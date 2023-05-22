using System.Security.Claims;
using System.Threading.RateLimiting;

namespace Jordnaer.Server.Extensions;

public static class RateLimitExtensions
{
    private const string PER_USER_RATELIMIT_POLICY = "PerUserRatelimit";
    private const string HEALTH_CHECK_RATELIMIT_POLICY = "HealthCheckRateLimit";
    private const string AUTH_RATELIMIT_POLICY = "AuthenticationRateLimit";
    private const string USER_SEARCH_RATELIMIT_POLICY = "UserSearchRateLimit";

    public static IServiceCollection AddRateLimiting(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(PER_USER_RATELIMIT_POLICY, context =>
            {
                // We always have a user name
                string username = context.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                return RateLimitPartition.GetFixedWindowLimiter(username, _ => new FixedWindowRateLimiterOptions
                {
                    // 10 messages per user per 10 seconds
                    Window = TimeSpan.FromSeconds(15),
                    AutoReplenishment = true,
                    PermitLimit = 50
                });
            });

            options.AddPolicy(HEALTH_CHECK_RATELIMIT_POLICY, context =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(HEALTH_CHECK_RATELIMIT_POLICY, _ => new FixedWindowRateLimiterOptions
                {
                    // 5 messages per 10 seconds
                    Window = TimeSpan.FromSeconds(10),
                    AutoReplenishment = true,
                    PermitLimit = 5
                });
            });

            options.AddPolicy(AUTH_RATELIMIT_POLICY, context =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(context.Session.Id, _ => new FixedWindowRateLimiterOptions
                {
                    // 10 messages per user per 10 seconds
                    Window = TimeSpan.FromSeconds(10),
                    AutoReplenishment = true,
                    PermitLimit = 10
                });
            });

            options.AddPolicy(USER_SEARCH_RATELIMIT_POLICY,
                context => RateLimitPartition.GetFixedWindowLimiter(context.Session.Id, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromSeconds(10),
                        PermitLimit = 3,
                        AutoReplenishment = true
                    }));
        });

    public static IEndpointConventionBuilder RequirePerUserRateLimit(this IEndpointConventionBuilder builder) => builder.RequireRateLimiting(PER_USER_RATELIMIT_POLICY);
    public static IEndpointConventionBuilder RequireHealthCheckRateLimit(this IEndpointConventionBuilder builder) => builder.RequireRateLimiting(HEALTH_CHECK_RATELIMIT_POLICY);
    public static IEndpointConventionBuilder RequireAuthRateLimit(this IEndpointConventionBuilder builder) => builder.RequireRateLimiting(AUTH_RATELIMIT_POLICY);
    public static IEndpointConventionBuilder RequireUserSearchRateLimit(this IEndpointConventionBuilder builder) => builder.RequireRateLimiting(USER_SEARCH_RATELIMIT_POLICY);
}
