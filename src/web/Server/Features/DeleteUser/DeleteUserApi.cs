using Jordnaer.Server.Authentication;
using Jordnaer.Server.Authorization;
using Jordnaer.Server.Database;
using Jordnaer.Server.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Jordnaer.Server.Features.DeleteUser;

public static class DeleteUserApi
{
    private const string TokenPurpose = "delete-user";

    public static RouteGroupBuilder MapDeleteUsers(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/delete-user");

        group.RequireAuthorization(builder => builder.RequireCurrentUser());

        group.RequirePerUserRateLimit();

        group.MapGet("{id}", async Task<bool> (
            HttpContext context,
            [FromRoute] string id,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] CurrentUser currentUser,
            [FromServices] ISendGridClient sendGridClient,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.Id != id)
            {
                return false;
            }

            var from = new EmailAddress("kontakt@mini-moeder.dk", "Kontakt @ Mini Møder");
            var to = new EmailAddress(currentUser.User!.Email);

            string token = await userManager.GenerateUserTokenAsync(currentUser.User, TokenOptions.DefaultProvider,
                TokenPurpose);

            string deletionLink = $"{context.Request.Scheme}://{context.Request.Host}/delete-user/{token}";

            string message = CreateDeleteUserEmailMessage(deletionLink);

            var email = new SendGridMessage
            {
                From = from, // Must be from a verified email
                Subject = "Anmodning om sletning af bruger",
                HtmlContent = message,
                ReplyTo = to,
            };

            var emailSentResponse = await sendGridClient.SendEmailAsync(email, cancellationToken);

            return emailSentResponse.IsSuccessStatusCode;
        });

        group.MapDelete("{id}", async Task<Results<UnauthorizedHttpResult, Ok>> (
            HttpContext httpContext,
            [FromRoute] string id,
            [FromServices] IDeleteUserService deleteUserService,
            [FromServices] CurrentUser currentUser) =>
        {
            if (currentUser.Id != id)
                return TypedResults.Unauthorized();

            bool userDeleted = await deleteUserService.DeleteUserAsync(id);
            if (!userDeleted)
                return TypedResults.Unauthorized();

            await httpContext.SignOutFromAllAccountsAsync();

            return TypedResults.Ok();
        });

        return group;
    }

    private static string CreateDeleteUserEmailMessage(string deletionLink) =>
        $@"
<p>Hej,</p>

<p>Du har anmodet om at slette din bruger hos Mini Møder. Hvis du fortsætter, vil alle dine data blive permanent slettet og kan ikke genoprettes.</p>

<p>Hvis du er sikker på, at du vil slette din bruger, skal du klikke på linket nedenfor:</p>

<p><a href=""{deletionLink}"">Bekræft sletning af bruger</a></p>

<p>Hvis du ikke anmodede om at slette din bruger, kan du ignorere denne e-mail.</p>

<p>Med venlig hilsen,</p>

<p>Mini Møder-teamet</p>
";
}
