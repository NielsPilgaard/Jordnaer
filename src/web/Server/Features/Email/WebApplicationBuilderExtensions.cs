using Polly;
using SendGrid.Extensions.DependencyInjection;

namespace Jordnaer.Server.Features.Email;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddEmailServices(this WebApplicationBuilder builder)
    {
        string sendGridApiKey = builder.Configuration.GetValue<string>("SendGrid:ApiKey")!;

        builder.Services
            .AddSendGrid(options => options.ApiKey = sendGridApiKey)
            .AddTransientHttpErrorPolicy(policyBuilder =>
                policyBuilder.WaitAndRetryAsync(3, retryCount => TimeSpan.FromMilliseconds(50 * retryCount)))
            .AddTransientHttpErrorPolicy(policyBuilder =>
                policyBuilder.CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromSeconds(10)));

        builder.Services
            .AddHealthChecks()
            .AddSendGrid(sendGridApiKey);

        return builder;
    }
}
