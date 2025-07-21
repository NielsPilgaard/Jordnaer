using Azure;
using Azure.Communication.Email;
using Jordnaer.Extensions;
using Jordnaer.Features.Email;
using Jordnaer.Features.Metrics;
using MassTransit;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Jordnaer.Consumers;

public class SendEmailConsumer(
	ILogger<SendEmailConsumer> logger,
	EmailClient emailClient)
	: IConsumer<SendEmail>
{
	private static readonly ResiliencePipeline<EmailSendOperation> Retry =
		new ResiliencePipelineBuilder<EmailSendOperation>()
		.AddRetry(new RetryStrategyOptions<EmailSendOperation>
		{
			ShouldHandle = new PredicateBuilder<EmailSendOperation>()
				.Handle<Exception>()
				.HandleResult(x => x.HasValue is false)
				.HandleResult(x => x.GetRawResponse().IsError),
			BackoffType = DelayBackoffType.Exponential,
			Delay = TimeSpan.FromSeconds(1),
			UseJitter = true,
			MaxDelay = TimeSpan.FromMinutes(5),
			MaxRetryAttempts = 15,
			Name = "AzureEmailRetry"
		})
		.AddCircuitBreaker(new CircuitBreakerStrategyOptions<EmailSendOperation>
		{
			FailureRatio = 0.20,
			SamplingDuration = TimeSpan.FromSeconds(30),
			MinimumThroughput = 5,
			BreakDurationGenerator = static args => new ValueTask<TimeSpan>(TimeSpan.FromSeconds(args.FailureCount * 30)),
		})
		.Build();

	public async Task Consume(ConsumeContext<SendEmail> consumeContext)
	{
		logger.LogFunctionBegan();
		var message = consumeContext.Message;

		var emailMessage = new EmailMessage(senderAddress: message.From?.Email ?? EmailConstants.ContactEmail.Email,
											content: new EmailContent(message.Subject)
											{
												Html = message.HtmlContent
											},
											recipients: new EmailRecipients(
												to: message.To?.Select(ConvertToEmailAddress).ToList(),
												bcc: message.Bcc?.Select(ConvertToEmailAddress).ToList()
											))
		{ UserEngagementTrackingDisabled = message.DisableUserEngagementTracking };

		// Set reply-to if specified
		if (message.ReplyTo is not null)
		{
			emailMessage.ReplyTo.Add(ConvertToEmailAddress(message.ReplyTo));
		}

		try
		{
			var response = await Retry.ExecuteAsync(
				async token => await emailClient.SendAsync(WaitUntil.Completed, emailMessage, token),
				consumeContext.CancellationToken);

			var rawResponse = response.GetRawResponse();

			if (rawResponse.IsError)
			{
				logger.LogError("Failed to send email to {Recipients}. " +
							   "StatusCode: {StatusCode}. " +
							   "Response: {Response}. " +
							   "Email: {Email}",
							   message.GetAllRecipients(),
							   rawResponse.Status,
							   rawResponse.ReasonPhrase,
							   message);
			}
			else
			{
				logger.LogInformation("Email(s) sent to: {Recipients}. MessageId: {MessageId}",
									  message.GetAllRecipients(), response.Id);

				JordnaerMetrics.EmailsSentCounter.Add(1);
			}
		}
		catch (RequestFailedException exception)
		{
			logger.LogError(exception, "Failed to send email to {Recipients}. " +
									   "ErrorCode: {ErrorCode}. " +
									   "Message: {Message}. " +
									   "Sender: {Email}",
							message.GetAllRecipients(),
							exception.ErrorCode,
							exception.Message,
							message.From?.ToString());
			throw;
		}
	}

	private static EmailAddress ConvertToEmailAddress(EmailRecipient recipient)
	{
		return string.IsNullOrWhiteSpace(recipient.DisplayName)
			? new EmailAddress(recipient.Email)
			: new EmailAddress(recipient.Email, recipient.DisplayName);
	}
}