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

		var subject = $"Partner henvendelse fra {senderName}";

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
			HtmlContent = htmlContent,
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

		var groupMembershipUrl = $"{options.Value.BaseUrl}/groups/{Uri.EscapeDataString(groupName)}/members";

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

		var groupUrl = $"{options.Value.BaseUrl}/groups/{Uri.EscapeDataString(groupName)}";

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

		var approvalUrl = $"{options.Value.BaseUrl}/backoffice/partners/{partnerId}";

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

		if (!string.IsNullOrEmpty(partner.PendingLink))
		{
			var encodedLink = WebUtility.HtmlEncode(partner.PendingLink);
			changesList.Add($"<li>Nyt link: {encodedLink}</li>");
		}

		var encodedPartnerName = WebUtility.HtmlEncode(partnerName);

		var email = new SendEmail
		{
			Subject = $"Ny partner godkendelse: {partnerName}",
			HtmlContent = $"""
						  <h4>Partner <b>{encodedPartnerName}</b> har uploadet nye ændringer til godkendelse</h4>

						  <p>Ændringer:</p>
						  <ul>
						  {string.Join("\n", changesList)}
						  </ul>

						  <p><a href="{approvalUrl}">Klik her for at godkende eller afvise ændringerne</a></p>

						  {EmailConstants.Signature}
						  """,
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

		var loginUrl = $"{options.Value.BaseUrl}/Account/Login";
		var dashboardUrl = $"{options.Value.BaseUrl}/partner/dashboard";

		var encodedPartnerName = WebUtility.HtmlEncode(partnerName);
		var encodedEmail = WebUtility.HtmlEncode(email);
		var encodedTemporaryPassword = WebUtility.HtmlEncode(temporaryPassword);

		var welcomeEmail = new SendEmail
		{
			Subject = "Velkommen som partner på Mini Møder",
			HtmlContent = $"""
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
						      <li><a href="{loginUrl}">Log ind med dine oplysninger</a></li>
						      <li>Skift dit kodeord under <em>Profil i øverste højre hjørne → Kontoindstillinger → Adgangskode</em></li>
						      <li>Gå til <a href="{dashboardUrl}">dit partner dashboard</a> for at uploade annoncer og se statistik</li>
						  </ol>

						  <p>Hvis du har spørgsmål eller brug for hjælp, er du velkommen til at kontakte os.</p>

						  {EmailConstants.Signature}
						  """,
			To = [new EmailRecipient { Email = email, DisplayName = encodedPartnerName }]
		};

		await publishEndpoint.Publish(welcomeEmail, cancellationToken);
	}
}