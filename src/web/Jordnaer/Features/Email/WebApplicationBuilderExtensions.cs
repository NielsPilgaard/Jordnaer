using Polly;
using Polly.Contrib.WaitAndRetry;
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
                policyBuilder.WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(500), 3)));

        builder.Services
            .AddHealthChecks()
            .AddSendGrid(sendGridApiKey);

        return builder;
    }
}
