using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Shared;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Jordnaer.Features.Email;

public interface IEmailService
{
	Task SendEmailFromContactForm(ContactForm contactForm, CancellationToken cancellationToken = default);
	Task SendMembershipRequestEmails(string groupName, CancellationToken cancellationToken = default);
	Task SendGroupInviteEmail(string groupName, string userId, CancellationToken cancellationToken = default);
}

public sealed class EmailService(IPublishEndpoint publishEndpoint,
	IDbContextFactory<JordnaerDbContext> contextFactory,
	ILogger<EmailService> logger,
	IOptions<AppOptions> options) : IEmailService
{
	public async Task SendEmailFromContactForm(
		ContactForm contactForm,
		CancellationToken cancellationToken = default)
	{
		var replyTo = new EmailRecipient { Email = contactForm.Email, DisplayName = contactForm.Name };

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
		string groupName,
		CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var membersThatCanApproveRequest = context
										   .GroupMemberships
										   .AsNoTracking()
										   .Where(x =>
													  x.Group.Name == groupName &&
													  x.PermissionLevel == PermissionLevel.Admin)
										   .Select(x => x.UserProfileId);

		var emails = await context.Users
							.AsNoTracking()
							.Where(user => membersThatCanApproveRequest.Any(userId => userId == user.Id))
							.Select(user => new EmailRecipient
							{
								Email = user.Email!,
								DisplayName = user.UserName
							})
							.ToListAsync(cancellationToken);

		if (emails.Count is 0)
		{
			logger.LogError("No users found who can approve membership request for group {GroupName}.", groupName);
			return;
		}

		logger.LogInformation("Found {MembersThatCanApproveRequestCount} users who can approve " +
							  "membership request for group {GroupName}. " +
							  "Sending an email to them.", emails.Count, groupName);

		var groupMembershipUrl = $"{options.Value.BaseUrl}/groups/{groupName}/members";

		var email = new SendEmail
		{
			Subject = $"Ny medlemskabsanmodning til {groupName}",
			HtmlContent = $"""
						  <h4>Din gruppe <b>{groupName}</b> har modtaget en ny medlemskabsanmodning</h4>

						  <a href="{groupMembershipUrl}">Klik her for at se den</a>

						  {EmailConstants.Signature}
						  """,
			Bcc = emails.ToList()
		};

		await publishEndpoint.Publish(email, cancellationToken);
	}

	public async Task SendGroupInviteEmail(
		string groupName,
		string userId,
		CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		// Get the user being invited
		var invitedUser = await context.Users
			.AsNoTracking()
			.Where(x => x.Id == userId)
			.Select(x => new EmailRecipient
			{
				Email = x.Email!,
				DisplayName = x.UserName
			})
			.FirstOrDefaultAsync(cancellationToken);

		if (invitedUser is null)
		{
			logger.LogError("User {UserId} not found for group invite to {GroupName}.", userId, groupName);
			return;
		}

		logger.LogInformation("Sending group invite email to user {UserId} for group {GroupName}.", userId, groupName);

		var groupUrl = $"{options.Value.BaseUrl}/groups/{groupName}";

		var email = new SendEmail
		{
			Subject = $"Du er inviteret til {groupName}",
			HtmlContent = $"""
						  <h4>Du er blevet inviteret til at blive medlem af gruppen <b>{groupName}</b></h4>

						  <a href="{groupUrl}">Klik her for at se gruppen og acceptere invitationen</a>

						  {EmailConstants.Signature}
						  """,
			To = [invitedUser]
		};

		await publishEndpoint.Publish(email, cancellationToken);
	}
}