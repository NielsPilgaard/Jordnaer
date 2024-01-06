using Jordnaer.Extensions;
using Polly;
using Polly.Contrib.WaitAndRetry;
using SendGrid.Extensions.DependencyInjection;

namespace Jordnaer.Features.Email;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddEmailServices(this WebApplicationBuilder builder, string baseUrl)
	{
		var sendGridApiKey = builder.Configuration.GetValue<string>("SendGrid:ApiKey")!;

		builder.Services
			.AddSendGrid(options => options.ApiKey = sendGridApiKey)
			.AddTransientHttpErrorPolicy(policyBuilder =>
				policyBuilder.WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(500), 3)));

		builder.Services
			.AddHealthChecks()
			.AddSendGrid(sendGridApiKey);

		builder.Services.AddRefitClient<IEmailClient>(baseUrl);

		return builder;
	}
}
