using Jordnaer.Database;
using Microsoft.AspNetCore.Identity;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Jordnaer.Features.Email;

public class SendGridEmailSender : IEmailSender<ApplicationUser>
{
	private readonly ISendGridClient _sendGridClient;
	private readonly ILogger<SendGridEmailSender> _logger;

	public SendGridEmailSender(ISendGridClient sendGridClient, ILogger<SendGridEmailSender> logger)
	{
		_sendGridClient = sendGridClient;
		_logger = logger;
	}

	internal async Task Send(string email, string subject, string message)
	{
		var msg = new SendGridMessage
		{
			From = EmailConstants.ContactEmail,
			Subject = subject,
			HtmlContent = message
		};

		var to = new EmailAddress(email);
		msg.AddTo(to);

		msg.TrackingSettings = new TrackingSettings
		{
			ClickTracking = new ClickTracking { Enable = false },
			Ganalytics = new Ganalytics { Enable = false },
			OpenTracking = new OpenTracking { Enable = false },
			SubscriptionTracking = new SubscriptionTracking { Enable = false }
		};

		// TODO: This needs to go on a queue
		var response = await _sendGridClient.SendEmailAsync(msg);
		if (response.IsSuccessStatusCode)
		{
			_logger.LogInformation("Email sent successfully");
		}
		else
		{
			_logger.LogError("Failed to send email to {Email}. StatusCode: {StatusCode}. Response: {Response}",
							 email, response.StatusCode.ToString(), await response.Body.ReadAsStringAsync());
		}
	}

	public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
	{
		const string subject = "Bekræft din konto på Mini Møder";
		var message = $"""
					   <p>Hej {user.UserName},</p>
					   
					   <p>Tak for at du registrerer dig hos Mini Møder.</p>
					   <p>Klik venligst på nedenstående link for at bekræfte din konto:</p>
					   
					   <a href="{confirmationLink}">Bekræft din konto</a>
					   
					   <p>Venlig hilsen,<br>
					   Mini Møder Teamet</p>
					   """;

		await Send(email, subject, message);
	}

	public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
	{
		const string subject = "Nulstil din adgangskode for Mini Møder";
		var message = $"""
		              <p>Hej {user.UserName},</p>
		              
		              <p>Vi har modtaget en anmodning om at nulstille din adgangskode.</p>
		              <p>Klik på linket nedenfor for at indstille en ny adgangskode:</p>
		              
		              <a href="{resetLink}">Nulstil adgangskode</a>
		              
		              <p>Hvis du ikke anmodede om at nulstille din adgangskode, bedes du ignorere denne e-mail.</p>
		              
		              <p>Venlig hilsen,<br>
		              Mini Møder Teamet</p>
		              
		              """;

		await Send(email, subject, message);
	}

	public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
	{
		const string subject = "Din kode til at nulstille din adgangskode for Mini Møder";
		var message = $"""
		              <p>Hej {user.UserName},</p>
		              
		              <p>Din kode til at nulstille adgangskoden er: <strong>{resetCode}</strong></p>
		              
		              <p>Indtast denne kode i formularen for at nulstille din adgangskode.</p>
		              
		              <p>Venlig hilsen,<br>
		              Mini Møder Teamet</p>
		              """;

		await Send(email, subject, message);
	}
}