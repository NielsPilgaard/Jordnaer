using Jordnaer.Extensions;
using Jordnaer.Features.Email;
using MassTransit;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using SendGrid;
using SendGrid.Helpers.Mail;
using Response = SendGrid.Response;

namespace Jordnaer.Consumers;

public class SendEmailConsumer(
	ILogger<SendMessageConsumer> logger,
	ISendGridClient sendGridClient)
	: IConsumer<SendEmail>
{
	private static readonly ResiliencePipeline<Response> Retry = new ResiliencePipelineBuilder<Response>()
		.AddRetry(new RetryStrategyOptions<Response>
		{
			ShouldHandle = new PredicateBuilder<Response>()
				.Handle<Exception>()
				.HandleResult(x => !x.IsSuccessStatusCode),
			BackoffType = DelayBackoffType.Exponential,
			Delay = TimeSpan.FromSeconds(1),
			UseJitter = true,
			MaxDelay = TimeSpan.FromSeconds(30),
			MaxRetryAttempts = 8,
			Name = "SendGridRetry"
		})
		.AddCircuitBreaker(new CircuitBreakerStrategyOptions<Response>
		{
			FailureRatio = 0.20,
			SamplingDuration = TimeSpan.FromSeconds(30),
			MinimumThroughput = 5,
			BreakDurationGenerator = static args => new ValueTask<TimeSpan>(TimeSpan.FromSeconds(args.FailureCount * 30)),
		})
		.Build();

	private static readonly TrackingSettings DefaultTrackingSettings = new()
	{
		ClickTracking = new ClickTracking { Enable = false },
		Ganalytics = new Ganalytics { Enable = false },
		OpenTracking = new OpenTracking { Enable = false },
		SubscriptionTracking = new SubscriptionTracking { Enable = false }
	};

	public async Task Consume(ConsumeContext<SendEmail> consumeContext)
	{
		logger.LogFunctionBegan();

		var message = consumeContext.Message;
		var email = new SendGridMessage
		{
			From = message.From ?? EmailConstants.ContactEmail, // Must be from a verified email
			Subject = message.Subject,
			HtmlContent = message.HtmlContent
		};

		if (message.To?.Count is > 0)
		{
			email.AddTos(message.To);
		}

		if (message.ReplyTo is not null)
		{
			email.ReplyTo = message.ReplyTo;
		}

		if (message.Bcc?.Count is > 0)
		{
			email.AddBccs(message.Bcc);
		}

		email.TrackingSettings = message.TrackingSettings ?? DefaultTrackingSettings;

		var response = await Retry.ExecuteAsync(
						   async token => await sendGridClient.SendEmailAsync(email, token),
						   consumeContext.CancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			logger.LogError("Failed to send email to {@Recipient}. " +
							"StatusCode: {StatusCode}. " +
							"Response: {Response}. " +
							"Email: {Email}",
							message.To, response.StatusCode.ToString(),
							await response.Body.ReadAsStringAsync(), message);
		}
		else
		{
			logger.LogInformation("Email sent to {@Recipient}. Subject: {Subject}", message.To, message.Subject);
		}
	}
}