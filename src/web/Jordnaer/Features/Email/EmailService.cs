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
	Task SendEmailFromPartnerContactForm(PartnerContactForm partnerContactForm, CancellationToken cancellationToken = default);
	Task SendMembershipRequestEmails(string groupName, CancellationToken cancellationToken = default);
	Task SendGroupInviteEmail(string groupName, string userId, CancellationToken cancellationToken = default);
	Task SendGroupInviteEmailToNewUserAsync(string email, string groupName, string inviteToken, CancellationToken cancellationToken = default);
	Task SendPartnerImageApprovalEmailAsync(Guid partnerId, string partnerName, CancellationToken cancellationToken = default);
	Task SendPartnerWelcomeEmailAsync(string email, string partnerName, string temporaryPassword, CancellationToken cancellationToken = default);
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

	public async Task SendEmailFromPartnerContactForm(
		PartnerContactForm partnerContactForm,
		CancellationToken cancellationToken = default)
	{
		var replyTo = new EmailRecipient { Email = partnerContactForm.Email, DisplayName = partnerContactForm.ContactPersonName };

		var senderName = !string.IsNullOrWhiteSpace(partnerContactForm.CompanyName)
			? partnerContactForm.CompanyName
			: partnerContactForm.ContactPersonName;

		var email = new SendEmail
		{
			Subject = $"Partner henvendelse fra {senderName.Trim()}",
			ReplyTo = replyTo,
			HtmlContent = EmailContentBuilder.PartnerContactForm(
				options.Value.BaseUrl,
				partnerContactForm.CompanyName,
				partnerContactForm.ContactPersonName,
				partnerContactForm.Email,
				partnerContactForm.PhoneNumber,
				partnerContactForm.Message),
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
							.Where(user => membersThatCanApproveRequest.Any(userId => userId == user.Id) &&
											!string.IsNullOrEmpty(user.Email))
							.Select(user => new EmailRecipient
							{
								Email = user.Email!, // Safe: filtered by user.Email != null above
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

		var email = new SendEmail
		{
			Subject = $"Ny medlemskabsanmodning til {groupName}",
			HtmlContent = EmailContentBuilder.MembershipRequest(options.Value.BaseUrl, groupName),
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
			.Where(x => x.Id == userId && x.Email != null)
			.Select(x => new EmailRecipient
			{
				Email = x.Email!, // Safe: filtered by x.Email != null above
				DisplayName = x.UserName
			})
			.FirstOrDefaultAsync(cancellationToken);

		if (invitedUser is null)
		{
			logger.LogError("User {UserId} not found for group invite to {GroupName}.", userId, groupName);
			return;
		}

		logger.LogInformation("Sending group invite email to user {UserId} for group {GroupName}.", userId, groupName);

		var email = new SendEmail
		{
			Subject = $"Du er inviteret til {groupName}",
			HtmlContent = EmailContentBuilder.GroupInvite(options.Value.BaseUrl, groupName),
			To = [invitedUser]
		};

		await publishEndpoint.Publish(email, cancellationToken);
	}

	public async Task SendGroupInviteEmailToNewUserAsync(
		string email,
		string groupName,
		string inviteToken,
		CancellationToken cancellationToken = default)
	{
		logger.LogInformation("Sending group invite email to new user {Email} for group {GroupName}.", new MaskedEmail(email), groupName);

		var inviteEmail = new SendEmail
		{
			Subject = $"Du er inviteret til {groupName} på Mini Møder",
			HtmlContent = EmailContentBuilder.GroupInviteNewUser(options.Value.BaseUrl, groupName, inviteToken),
			To = [new EmailRecipient { Email = email }]
		};

		await publishEndpoint.Publish(inviteEmail, cancellationToken);
	}

	public async Task SendPartnerImageApprovalEmailAsync(
		Guid partnerId,
		string partnerName,
		CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var partner = await context.Partners
			.AsNoTracking()
			.FirstOrDefaultAsync(s => s.Id == partnerId, cancellationToken);

		if (partner is null)
		{
			logger.LogError("Partner {PartnerId} not found when trying to send approval email", partnerId);
			return;
		}

		logger.LogInformation("Sending partner image approval email for partner {PartnerName} ({PartnerId})", partnerName, partnerId);

		var baseUrl = options.Value.BaseUrl;

		var changesList = new List<string>();

		if (!string.IsNullOrEmpty(partner.PendingAdImageUrl))
		{
			changesList.Add($"Nyt annonce billede: {partner.PendingAdImageUrl}");
		}

		if (!string.IsNullOrEmpty(partner.PendingLogoUrl))
		{
			changesList.Add($"Nyt logo: {partner.PendingLogoUrl}");
		}

		if (!string.IsNullOrEmpty(partner.PendingName))
		{
			changesList.Add($"Nyt navn: {partner.PendingName}");
		}

		if (!string.IsNullOrEmpty(partner.PendingDescription))
		{
			changesList.Add($"Ny beskrivelse: {partner.PendingDescription}");
		}

		if (!string.IsNullOrEmpty(partner.PendingPartnerPageLink))
		{
			changesList.Add($"Nyt partnerside link: {partner.PendingPartnerPageLink}");
		}

		if (!string.IsNullOrEmpty(partner.PendingAdLink))
		{
			changesList.Add($"Nyt annonce link: {partner.PendingAdLink}");
		}

		if (!string.IsNullOrEmpty(partner.PendingAdLabelColor))
		{
			changesList.Add($"Ny annonce label farve: {partner.PendingAdLabelColor}");
		}

		var email = new SendEmail
		{
			Subject = $"Ny partner godkendelse: {partnerName}",
			HtmlContent = EmailContentBuilder.PartnerImageApproval(baseUrl, partnerName, partnerId, changesList),
			To = [new EmailRecipient { Email = "kontakt@mini-moeder.dk", DisplayName = "Mini Møder Admin" }]
		};

		await publishEndpoint.Publish(email, cancellationToken);
	}

	public async Task SendPartnerWelcomeEmailAsync(
		string email,
		string partnerName,
		string temporaryPassword,
		CancellationToken cancellationToken = default)
	{
		logger.LogInformation("Sending partner welcome email to {Email} for partner {PartnerName}", new MaskedEmail(email), partnerName);

		var welcomeEmail = new SendEmail
		{
			Subject = "Velkommen som partner på Mini Møder",
			HtmlContent = EmailContentBuilder.PartnerWelcome(options.Value.BaseUrl, partnerName, email, temporaryPassword),
			To = [new EmailRecipient { Email = email, DisplayName = partnerName }]
		};

		await publishEndpoint.Publish(welcomeEmail, cancellationToken);
	}
}
