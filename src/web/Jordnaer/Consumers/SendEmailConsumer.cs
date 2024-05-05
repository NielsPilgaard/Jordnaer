using Jordnaer.Extensions;
using Jordnaer.Features.Email;
using MassTransit;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Jordnaer.Consumers;

public class SendEmailConsumer(
	ILogger<SendMessageConsumer> logger,
	ISendGridClient sendGridClient)
	: IConsumer<SendEmail>
{
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

		email.AddTo(message.To);

		if (message.ReplyTo is not null)
		{
			email.ReplyTo = message.ReplyTo;
		}

		email.TrackingSettings = message.TrackingSettings ?? DefaultTrackingSettings;

		var response = await sendGridClient.SendEmailAsync(email, consumeContext.CancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			logger.LogError("Failed to send email to {Recipient}. " +
							"StatusCode: {StatusCode}. " +
							"Response: {Response}. " +
							"Email: {Email}",
							message.To.Email, response.StatusCode.ToString(),
							await response.Body.ReadAsStringAsync(), message);
		}
		else
		{
			logger.LogInformation("Email sent to {Recipient}. Subject: {Subject}", message.To.Email, message.Subject);
		}
	}
}