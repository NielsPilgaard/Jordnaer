using System.Security.Claims;
using System.Threading.RateLimiting;

namespace Jordnaer.Server.Extensions;

public static class RateLimitExtensions
{
    private const string Per_User_Ratelimit_Policy = "PerUserRateLimit";
    private const string Health_Check_Ratelimit_Policy = "HealthCheckRateLimit";
    private const string Auth_Ratelimit_Policy = "AuthenticationRateLimit";
    private const string User_Search_Ratelimit_Policy = "UserSearchRateLimit";

    private const string Anonymous_Partition = "Anonymous";

    public static IServiceCollection AddRateLimiting(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(Per_User_Ratelimit_Policy, context =>
            {
                string? username = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                return username is not null
                    ? RateLimitPartition.GetFixedWindowLimiter(username, _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromSeconds(15),
                        PermitLimit = 50
                    })
                    : RateLimitPartition.GetFixedWindowLimiter(Anonymous_Partition, _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromSeconds(1),
                        PermitLimit = 50
                    });
            });

            options.AddPolicy(Health_Check_Ratelimit_Policy, _ =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(Health_Check_Ratelimit_Policy, _ => new FixedWindowRateLimiterOptions
                {
                    // 5 messages per 10 seconds
                    Window = TimeSpan.FromSeconds(10),
                    PermitLimit = 5
                });
            });

            options.AddPolicy(Auth_Ratelimit_Policy, context =>
            {
                string? username = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                return username is null
                    ? RateLimitPartition.GetFixedWindowLimiter(Anonymous_Partition,
                        _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromSeconds(1), PermitLimit = 50 })
                    : RateLimitPartition.GetNoLimiter(username);
            });

            options.AddPolicy(User_Search_Ratelimit_Policy,
                context =>
                {
                    string? username = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                    return username is not null
                        ? RateLimitPartition.GetFixedWindowLimiter(username, _ => new FixedWindowRateLimiterOptions
                        {
                            Window = TimeSpan.FromSeconds(10),
                            PermitLimit = 15
                        })
                        : RateLimitPartition.GetFixedWindowLimiter(Anonymous_Partition, _ => new FixedWindowRateLimiterOptions
                        {
                            Window = TimeSpan.FromSeconds(1),
                            PermitLimit = 15
                        });
                });
        });

    public static IEndpointConventionBuilder RequirePerUserRateLimit(this IEndpointConventionBuilder builder) => builder.RequireRateLimiting(Per_User_Ratelimit_Policy);
    public static IEndpointConventionBuilder RequireHealthCheckRateLimit(this IEndpointConventionBuilder builder) => builder.RequireRateLimiting(Health_Check_Ratelimit_Policy);
    public static IEndpointConventionBuilder RequireAuthRateLimit(this IEndpointConventionBuilder builder) => builder.RequireRateLimiting(Auth_Ratelimit_Policy);
    public static IEndpointConventionBuilder RequireUserSearchRateLimit(this IEndpointConventionBuilder builder) => builder.RequireRateLimiting(User_Search_Ratelimit_Policy);
}
