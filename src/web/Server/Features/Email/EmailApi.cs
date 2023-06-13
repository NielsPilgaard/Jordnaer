using Jordnaer.Server.Extensions;
using Jordnaer.Shared.Email;
using Microsoft.AspNetCore.Mvc;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Jordnaer.Server.Features.Email;

public static class EmailApi
{
    public static RouteGroupBuilder MapEmail(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/email");

        group.RequirePerUserRateLimit();

        // TODO: This should send a message through an exchange to an Azure Function, which does the heavy lifting
        group.MapPost("contact", async Task<bool> (
            [FromBody] ContactForm contactFormModel,
            [FromServices] ISendGridClient sendGridClient,
            CancellationToken cancellationToken) =>
        {
            var contactEmail = new EmailAddress("kontakt@mini-moeder.dk", "Kontakt @ Mini Møder");
            var replyTo = new EmailAddress(contactFormModel.Email, contactFormModel.Name);

            string subject = contactFormModel.Name is null
                ? "Kontaktformular"
                : $"Kontaktformular besked fra {contactFormModel.Name}";

            var email = new SendGridMessage
            {
                From = contactEmail, // Must be from a verified email
                Subject = subject,
                PlainTextContent = contactFormModel.Message,
                ReplyTo = replyTo,
            };

            email.AddTo(contactEmail);

            var response = await sendGridClient.SendEmailAsync(email, cancellationToken);

            return response.IsSuccessStatusCode;
        });

        return group;
    }
}
