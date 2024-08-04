using Jordnaer.Database;
using Jordnaer.Shared;
using MassTransit;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Mail;

namespace Jordnaer.Features.Email;

public interface IEmailService
{
	Task SendEmailFromContactForm(ContactForm contactForm, CancellationToken cancellationToken = default);
	Task SendMembershipRequestEmails(Guid groupId, CancellationToken cancellationToken = default);
}

public sealed class EmailService(IPublishEndpoint publishEndpoint,
	IDbContextFactory<JordnaerDbContext> contextFactory,
	ILogger<EmailService> logger,
	NavigationManager navigationManager) : IEmailService
{
	public async Task SendEmailFromContactForm(
		ContactForm contactForm,
		CancellationToken cancellationToken = default)
	{
		var replyTo = new EmailAddress(contactForm.Email, contactForm.Name);

		var subject = contactForm.Name is null
						  ? "Kontaktformular"
						  : $"Kontaktformular besked fra {contactForm.Name}";

		var email = new SendEmail
		{
			Subject = subject,
			ReplyTo = replyTo,
			HtmlContent = contactForm.Message,
			To = [EmailConstants.ContactEmail]
		};

		await publishEndpoint.Publish(email, cancellationToken);
	}

	public async Task SendMembershipRequestEmails(
		Guid groupId,
		CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var membersThatCanApproveRequest = context
										   .GroupMemberships
										   .AsNoTracking()
										   .Where(x =>
													  x.GroupId == groupId &&
													  x.PermissionLevel == PermissionLevel.Admin)
										   .Select(x => x.UserProfileId);

		var emails = await context.Users
							.AsNoTracking()
							.Where(user => membersThatCanApproveRequest.Any(userId => userId == user.Id))
							.Select(user => new EmailAddress(user.Email, user.UserName))
							.ToListAsync(cancellationToken);

		if (emails.Count is 0)
		{
			logger.LogError("No users found who can approve membership request for group {GroupId}.", groupId);
			return;
		}

		logger.LogInformation("Found {MembersThatCanApproveRequestCount} users who can approve " +
							  "membership request for group {GroupId}. " +
							  "Sending an email to them.", emails.Count, groupId);

		var groupMembershipUrl = $"{navigationManager.BaseUri}/groups/{groupId}/memberships";

		List<EmailAddress> toEmail = [emails.First()];
		var email = new SendEmail
		{
			Subject = "Ny medlemskabsanmodning",
			HtmlContent = $"""
						  <h4>Din gruppe har modtaget en ny medlemskabsanmodning</h4>

						  <a href="{groupMembershipUrl}">Klik her for at se den</a>

						  {EmailConstants.Signature}
						  """,
			Bcc = emails.Except(toEmail).ToList(),
			// No need to expose all emails to the moderators and admins of the group,
			// but we must have one "To" to send an email.
			To = toEmail
		};

		await publishEndpoint.Publish(email, cancellationToken);
	}
}