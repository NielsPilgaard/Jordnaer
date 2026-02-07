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
		var message = EmailContentBuilder.Confirmation(options.Value.BaseUrl, user.UserName, confirmationLink);
		await Send(email, "Bekræft din konto på Mini Møder", message);
	}

	public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
	{
		var message = EmailContentBuilder.PasswordResetLink(options.Value.BaseUrl, user.UserName, resetLink);
		await Send(email, "Nulstil din adgangskode for Mini Møder", message);
	}

	public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
	{
		var message = EmailContentBuilder.PasswordResetCode(options.Value.BaseUrl, user.UserName, resetCode);
		await Send(email, "Din kode til at nulstille din adgangskode for Mini Møder", message);
	}
}
