using JetBrains.Annotations;

namespace Jordnaer.Features.Email;

public class SendEmail
{
	[LanguageInjection(InjectedLanguage.HTML)]
	public required string HtmlContent { get; set; }

	public required string Subject { get; set; }

	public List<EmailRecipient>? To { get; set; }

	public List<EmailRecipient>? Bcc { get; set; }

	public EmailRecipient? ReplyTo { get; set; }

	/// <summary>
	/// If you omit this value, the email will be sent from the default email address <see cref="EmailConstants.ContactEmail"/>.
	/// </summary>
	public EmailRecipient? From { get; set; }

	/// <summary>
	/// Indicates whether user engagement tracking should be disabled for this request.
	/// Azure Communication Services has limited tracking options compared to SendGrid.
	/// </summary>
	public bool DisableUserEngagementTracking { get; set; } = true;

	/// <summary>
	/// Gets all recipients (To and Bcc) concatenated for GDPR-compliant logging.
	/// </summary>
	public string GetAllRecipients() => EmailRecipient.ConcatRecipients(To, Bcc);
}