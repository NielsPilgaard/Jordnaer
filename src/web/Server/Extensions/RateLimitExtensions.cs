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

                return RateLimitPartition.GetTokenBucketLimiter(username, _ => new TokenBucketRateLimiterOptions
                {
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    AutoReplenishment = true,
                    TokenLimit = 100,
                    TokensPerPeriod = 100,
                    QueueLimit = 100,
                });
            });

            options.AddPolicy(HEALTH_CHECK_RATELIMIT_POLICY, _ =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(PER_USER_RATELIMIT_POLICY, _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromSeconds(10),
                    AutoReplenishment = true,
                    QueueLimit = 5
                });
            });

            options.AddPolicy(AUTH_RATELIMIT_POLICY, _ =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(context.Session.Id, _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromSeconds(10),
                    AutoReplenishment = true,
                    QueueLimit = 10
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
