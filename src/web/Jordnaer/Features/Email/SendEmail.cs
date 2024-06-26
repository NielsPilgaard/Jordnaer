using JetBrains.Annotations;
using SendGrid.Helpers.Mail;

namespace Jordnaer.Features.Email;

public class SendEmail
{
	[LanguageInjection(InjectedLanguage.HTML)]
	public required string HtmlContent { get; set; }
	public required string Subject { get; set; }
	public List<EmailAddress>? To { get; set; }
	public List<EmailAddress>? Bcc { get; set; }
	public EmailAddress? ReplyTo { get; set; }

	/// <summary>
	/// If you omit this value, the email will be sent from the default email address <see cref="EmailConstants.ContactEmail"/>.
	/// </summary>
	public EmailAddress? From { get; set; }

	/// <summary>
	/// Action used to configure tracking settings for the email. By default, all tracking is off.
	/// </summary>
	public TrackingSettings? TrackingSettings { get; set; }
}