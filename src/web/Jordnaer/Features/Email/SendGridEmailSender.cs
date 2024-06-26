using Jordnaer.Database;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using SendGrid.Helpers.Mail;

namespace Jordnaer.Features.Email;

public class SendGridEmailSender(IPublishEndpoint publishEndpoint) : IEmailSender<ApplicationUser>
{
	internal async Task Send(string email, string subject, string message)
	{
		var sendEmail = new SendEmail
		{
			Subject = subject,
			HtmlContent = message,
			To = [new EmailAddress(email)]
		};

		await publishEndpoint.Publish(sendEmail);
	}

	public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
	{
		const string subject = "Bekræft din konto på Mini Møder";
		var message = $"""
					   {EmailConstants.Greeting(user.UserName)}
					   
					   <p>Tak for at du registrerer dig hos Mini Møder.</p>
					   <p>Klik venligst på nedenstående link for at bekræfte din konto:</p>
					   
					   <a href="{confirmationLink}">Bekræft din konto</a>
					   
					   {EmailConstants.Signature}
					   """;

		await Send(email, subject, message);
	}

	public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
	{
		const string subject = "Nulstil din adgangskode for Mini Møder";
		var message = $"""
		              {EmailConstants.Greeting(user.UserName)}
		              
		              <p>Vi har modtaget en anmodning om at nulstille din adgangskode.</p>
		              <p>Klik på linket nedenfor for at indstille en ny adgangskode:</p>
		              
		              <a href="{resetLink}">Nulstil adgangskode</a>
		              
		              <p>Hvis du ikke anmodede om at nulstille din adgangskode, bedes du ignorere denne e-mail.</p>
		              
		              {EmailConstants.Signature}
		              """;

		await Send(email, subject, message);
	}

	public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
	{
		const string subject = "Din kode til at nulstille din adgangskode for Mini Møder";
		var message = $"""
		              {EmailConstants.Greeting(user.UserName)}
		              
		              <p>Din kode til at nulstille adgangskoden er: <strong>{resetCode}</strong></p>
		              
		              <p>Indtast denne kode i formularen for at nulstille din adgangskode.</p>
		              
		              {EmailConstants.Signature}
		              """;

		await Send(email, subject, message);
	}
}