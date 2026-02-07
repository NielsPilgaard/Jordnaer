using Jordnaer.Database;
using Jordnaer.Extensions;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Jordnaer.Features.Email;

public class EmailSender(IPublishEndpoint publishEndpoint, IOptions<AppOptions> options) : IEmailSender<ApplicationUser>
{
	internal async Task Send(string email, string subject, string message)
	{
		var sendEmail = new SendEmail
		{
			Subject = subject,
			HtmlContent = message,
			To = [new EmailRecipient { Email = email }]
		};

		await publishEndpoint.Publish(sendEmail);
	}

	public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
	{
		const string subject = "Bekræft din konto på Mini Møder";
		var baseUrl = options.Value.BaseUrl;
		var body = $"""
				   {EmailConstants.Greeting(user.UserName)}

				   <p>Tak for at du registrerer dig hos Mini Møder.</p>
				   <p>Klik venligst på knappen nedenfor for at bekræfte din konto:</p>

				   {EmailTemplate.Button(confirmationLink, "Bekræft din konto")}
				   """;

		var message = EmailTemplate.Wrap(body, baseUrl, preheaderText: "Bekræft din Mini Møder konto");
		await Send(email, subject, message);
	}

	public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
	{
		const string subject = "Nulstil din adgangskode for Mini Møder";
		var baseUrl = options.Value.BaseUrl;
		var body = $"""
		           {EmailConstants.Greeting(user.UserName)}

		           <p>Vi har modtaget en anmodning om at nulstille din adgangskode.</p>
		           <p>Klik på knappen nedenfor for at indstille en ny adgangskode:</p>

		           {EmailTemplate.Button(resetLink, "Nulstil adgangskode")}

		           <p>Hvis du ikke anmodede om at nulstille din adgangskode, bedes du ignorere denne e-mail.</p>
		           """;

		var message = EmailTemplate.Wrap(body, baseUrl, preheaderText: "Nulstil din adgangskode");
		await Send(email, subject, message);
	}

	public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
	{
		const string subject = "Din kode til at nulstille din adgangskode for Mini Møder";
		var baseUrl = options.Value.BaseUrl;
		var body = $"""
		           {EmailConstants.Greeting(user.UserName)}

		           <p>Din kode til at nulstille adgangskoden er: <strong>{resetCode}</strong></p>

		           <p>Indtast denne kode i formularen for at nulstille din adgangskode.</p>
		           """;

		var message = EmailTemplate.Wrap(body, baseUrl);
		await Send(email, subject, message);
	}
}
