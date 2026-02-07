using System.Net;
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

		var subject = $"Partner henvendelse fra {WebUtility.HtmlEncode(senderName)}";

		var companyInfo = !string.IsNullOrWhiteSpace(partnerContactForm.CompanyName)
			? $"<p><strong>Firma:</strong> {WebUtility.HtmlEncode(partnerContactForm.CompanyName)}</p>"
			: "";

		var phoneInfo = !string.IsNullOrWhiteSpace(partnerContactForm.PhoneNumber)
			? $"<p><strong>Telefon:</strong> {WebUtility.HtmlEncode(partnerContactForm.PhoneNumber)}</p>"
			: "";

		var htmlContent = $"""
			<h4>Partner henvendelse</h4>

			{companyInfo}
			<p><strong>Kontaktperson:</strong> {WebUtility.HtmlEncode(partnerContactForm.ContactPersonName)}</p>
			<p><strong>Email:</strong> {WebUtility.HtmlEncode(partnerContactForm.Email)}</p>
			{phoneInfo}

			<h5>Besked:</h5>
			<p>{WebUtility.HtmlEncode(partnerContactForm.Message)}</p>
			""";

		var email = new SendEmail
		{
			Subject = subject,
			ReplyTo = replyTo,
			HtmlContent = EmailTemplate.Wrap(htmlContent, options.Value.BaseUrl),
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

		var baseUrl = options.Value.BaseUrl;
		var groupMembershipUrl = $"{baseUrl}/groups/{Uri.EscapeDataString(groupName)}/members";

		var body = $"""
				   <h4>Din gruppe <b>{groupName}</b> har modtaget en ny medlemskabsanmodning</h4>

				   {EmailTemplate.Button(groupMembershipUrl, "Se anmodningen")}
				   """;

		var email = new SendEmail
		{
			Subject = $"Ny medlemskabsanmodning til {groupName}",
			HtmlContent = EmailTemplate.Wrap(body, baseUrl, preheaderText: $"Ny medlemskabsanmodning til {groupName}"),
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

		var baseUrl = options.Value.BaseUrl;
		var groupUrl = $"{baseUrl}/groups/{Uri.EscapeDataString(groupName)}";

		var body = $"""
				   <h4>Du er blevet inviteret til at blive medlem af gruppen <b>{groupName}</b></h4>

				   {EmailTemplate.Button(groupUrl, "Se gruppen")}
				   """;

		var email = new SendEmail
		{
			Subject = $"Du er inviteret til {groupName}",
			HtmlContent = EmailTemplate.Wrap(body, baseUrl, preheaderText: $"Du er inviteret til {groupName}"),
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

		var baseUrl = options.Value.BaseUrl;
		var registerUrl = $"{baseUrl}/Account/Register?inviteToken={Uri.EscapeDataString(inviteToken)}";
		var encodedGroupName = WebUtility.HtmlEncode(groupName);

		var body = $"""
				   <h4>Du er blevet inviteret til at blive medlem af gruppen <b>{encodedGroupName}</b> på Mini Møder</h4>

				   <p>Opret en gratis konto for at acceptere invitationen og komme i kontakt med andre forældre i gruppen.</p>

				   {EmailTemplate.Button(registerUrl, "Opret konto og deltag")}

				   <p><small>Denne invitation udløber om 7 dage.</small></p>
				   """;

		var inviteEmail = new SendEmail
		{
			Subject = $"Du er inviteret til {groupName} på Mini Møder",
			HtmlContent = EmailTemplate.Wrap(body, baseUrl, preheaderText: $"Du er inviteret til {groupName} på Mini Møder"),
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
		var approvalUrl = $"{baseUrl}/backoffice/partners/{partnerId}";

		var changesList = new List<string>();

		if (!string.IsNullOrEmpty(partner.PendingAdImageUrl))
		{
			var encodedUrl = WebUtility.HtmlEncode(partner.PendingAdImageUrl);
			changesList.Add($"<li><a href=\"{encodedUrl}\">Nyt annonce billede</a></li>");
		}

		if (!string.IsNullOrEmpty(partner.PendingLogoUrl))
		{
			var encodedUrl = WebUtility.HtmlEncode(partner.PendingLogoUrl);
			changesList.Add($"<li><a href=\"{encodedUrl}\">Nyt logo</a></li>");
		}

		if (!string.IsNullOrEmpty(partner.PendingName))
		{
			var encodedName = WebUtility.HtmlEncode(partner.PendingName);
			changesList.Add($"<li>Nyt navn: {encodedName}</li>");
		}

		if (!string.IsNullOrEmpty(partner.PendingDescription))
		{
			var encodedDescription = WebUtility.HtmlEncode(partner.PendingDescription);
			changesList.Add($"<li>Ny beskrivelse: {encodedDescription}</li>");
		}

		if (!string.IsNullOrEmpty(partner.PendingPartnerPageLink))
		{
			var encodedLink = WebUtility.HtmlEncode(partner.PendingPartnerPageLink);
			changesList.Add($"<li>Nyt partnerside link: {encodedLink}</li>");
		}

		if (!string.IsNullOrEmpty(partner.PendingAdLink))
		{
			var encodedLink = WebUtility.HtmlEncode(partner.PendingAdLink);
			changesList.Add($"<li>Nyt annonce link: {encodedLink}</li>");
		}

		if (!string.IsNullOrEmpty(partner.PendingAdLabelColor))
		{
			var encodedColor = WebUtility.HtmlEncode(partner.PendingAdLabelColor);
			changesList.Add($"<li>Ny annonce label farve: {encodedColor}</li>");
		}

		var encodedPartnerName = WebUtility.HtmlEncode(partnerName);

		var body = $"""
				   <h4>Partner <b>{encodedPartnerName}</b> har uploadet nye ændringer til godkendelse</h4>

				   <p>Ændringer:</p>
				   <ul>
				   {string.Join("\n", changesList)}
				   </ul>

				   {EmailTemplate.Button(approvalUrl, "Godkend ændringer")}
				   """;

		var email = new SendEmail
		{
			Subject = $"Ny partner godkendelse: {partnerName}",
			HtmlContent = EmailTemplate.Wrap(body, baseUrl),
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

		var baseUrl = options.Value.BaseUrl;
		var loginUrl = $"{baseUrl}/Account/Login";
		var dashboardUrl = $"{baseUrl}/partner/dashboard";

		var encodedPartnerName = WebUtility.HtmlEncode(partnerName);
		var encodedEmail = WebUtility.HtmlEncode(email);
		var encodedTemporaryPassword = WebUtility.HtmlEncode(temporaryPassword);

		var body = $"""
				   <h4>Velkommen som partner på Mini Møder, {encodedPartnerName}!</h4>

				   <p>Din partnerkonto er blevet oprettet. Du kan nu logge ind og administrere dine partner-annoncer.</p>

				   <h5>Login oplysninger:</h5>
				   <ul>
				       <li><strong>Email:</strong> {encodedEmail}</li>
				       <li><strong>Midlertidigt kodeord:</strong> <code>{encodedTemporaryPassword}</code></li>
				   </ul>

				   <p><strong>VIGTIGT:</strong> Af sikkerhedsmæssige årsager bedes du ændre dit kodeord efter første login.</p>

				   <h5>Sådan kommer du i gang:</h5>
				   <ol>
				       <li>Log ind med dine oplysninger</li>
				       <li>Skift dit kodeord under <em>Profil i øverste højre hjørne → Kontoindstillinger → Adgangskode</em></li>
				       <li>Gå til dit partner dashboard for at uploade annoncer og se statistik</li>
				   </ol>

				   {EmailTemplate.Button(loginUrl, "Log ind")}
				   """;

		var welcomeEmail = new SendEmail
		{
			Subject = "Velkommen som partner på Mini Møder",
			HtmlContent = EmailTemplate.Wrap(body, baseUrl, preheaderText: "Velkommen som partner på Mini Møder"),
			To = [new EmailRecipient { Email = email, DisplayName = encodedPartnerName }]
		};

		await publishEndpoint.Publish(welcomeEmail, cancellationToken);
	}
}
