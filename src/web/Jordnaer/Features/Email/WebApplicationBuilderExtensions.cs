using Jordnaer.Database;
using Microsoft.AspNetCore.Identity;
using Polly;
using Polly.Contrib.WaitAndRetry;
using SendGrid.Extensions.DependencyInjection;

namespace Jordnaer.Features.Email;

public static class WebApplicationBuilderExtensions
{
	public static WebApplicationBuilder AddEmailServices(this WebApplicationBuilder builder)
	{
		builder.Services.AddScoped<IEmailSender<ApplicationUser>, SendGridEmailSender>();
		builder.Services.AddScoped<IEmailService, EmailService>();

		var sendGridApiKey = builder.Configuration.GetValue<string>("SendGrid:ApiKey")!;

		builder.Services
			.AddSendGrid(options => options.ApiKey = sendGridApiKey)
			.AddTransientHttpErrorPolicy(policyBuilder =>
				// TODO: Replace with Microsoft.Extensions.Resilience
				policyBuilder.WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(500), 3)));

		builder.Services
			.AddHealthChecks()
			.AddSendGrid(sendGridApiKey);

		return builder;
	}
}
