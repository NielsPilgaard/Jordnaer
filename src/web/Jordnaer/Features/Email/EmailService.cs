using Jordnaer.Shared;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Jordnaer.Features.Email;

public interface IEmailService
{
	Task<bool> SendEmailFromContactForm(ContactForm contactForm, CancellationToken cancellationToken = default);
}

public sealed class EmailService(ISendGridClient emailClient) : IEmailService
{
	public async Task<bool> SendEmailFromContactForm(ContactForm contactForm,
		CancellationToken cancellationToken = default)
	{
		var replyTo = new EmailAddress(contactForm.Email, contactForm.Name);

		var subject = contactForm.Name is null
						  ? "Kontaktformular"
						  : $"Kontaktformular besked fra {contactForm.Name}";

		var email = new SendGridMessage
		{
			From = EmailConstants.ContactEmail, // Must be from a verified email
			Subject = subject,
			PlainTextContent = contactForm.Message,
			ReplyTo = replyTo,
			TrackingSettings = new TrackingSettings
			{
				ClickTracking = new ClickTracking { Enable = false },
				Ganalytics = new Ganalytics { Enable = false },
				OpenTracking = new OpenTracking { Enable = false },
				SubscriptionTracking = new SubscriptionTracking { Enable = false }
			}
		};

		email.AddTo(EmailConstants.ContactEmail);

		var response = await emailClient.SendEmailAsync(email, cancellationToken);

		return response.IsSuccessStatusCode;
	}
}