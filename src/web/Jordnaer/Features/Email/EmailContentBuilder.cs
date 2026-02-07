using System.Net;

namespace Jordnaer.Features.Email;

/// <summary>
/// Builds the full HTML for each email type. Used by both the email services and the admin preview page.
/// </summary>
public static class EmailContentBuilder
{
	public static string Confirmation(string baseUrl, string? userName, string confirmationLink) =>
		EmailTemplate.Wrap($"""
			{EmailConstants.Greeting(userName)}

			<p>Tak for at du registrerer dig hos Mini Møder.</p>
			<p>Klik venligst på knappen nedenfor for at bekræfte din konto:</p>

			{EmailTemplate.Button(confirmationLink, "Bekræft din konto")}
			""", baseUrl, preheaderText: "Bekræft din Mini Møder konto");

	public static string PasswordResetLink(string baseUrl, string? userName, string resetLink) =>
		EmailTemplate.Wrap($"""
			{EmailConstants.Greeting(userName)}

			<p>Vi har modtaget en anmodning om at nulstille din adgangskode.</p>
			<p>Klik på knappen nedenfor for at indstille en ny adgangskode:</p>

			{EmailTemplate.Button(resetLink, "Nulstil adgangskode")}

			<p>Hvis du ikke anmodede om at nulstille din adgangskode, bedes du ignorere denne e-mail.</p>
			""", baseUrl, preheaderText: "Nulstil din adgangskode");

	public static string PasswordResetCode(string baseUrl, string? userName, string resetCode)
	{
		var encodedResetCode = WebUtility.HtmlEncode(resetCode);
		return EmailTemplate.Wrap($"""
			{EmailConstants.Greeting(userName)}

			<p>Din kode til at nulstille adgangskoden er: <strong>{encodedResetCode}</strong></p>

			<p>Indtast denne kode i formularen for at nulstille din adgangskode.</p>
			""", baseUrl);
	}

	public static string GroupInvite(string baseUrl, string groupName)
	{
		var groupUrl = $"{baseUrl}/groups/{Uri.EscapeDataString(groupName)}";
		var encodedGroupName = WebUtility.HtmlEncode(groupName);
		return EmailTemplate.Wrap($"""
			<h4>Du er blevet inviteret til at blive medlem af gruppen <b>{encodedGroupName}</b></h4>

			{EmailTemplate.Button(groupUrl, "Se gruppen")}
			""", baseUrl, preheaderText: $"Du er inviteret til {groupName}");
	}

	public static string GroupInviteNewUser(string baseUrl, string groupName, string inviteToken)
	{
		var registerUrl = $"{baseUrl}/Account/Register?inviteToken={Uri.EscapeDataString(inviteToken)}";
		var encodedGroupName = WebUtility.HtmlEncode(groupName);
		return EmailTemplate.Wrap($"""
			<h4>Du er blevet inviteret til at blive medlem af gruppen <b>{encodedGroupName}</b> på Mini Møder</h4>

			<p>Opret en gratis konto for at acceptere invitationen og komme i kontakt med andre forældre i gruppen.</p>

			{EmailTemplate.Button(registerUrl, "Opret konto og deltag")}

			<p><small>Denne invitation udløber om 7 dage.</small></p>
			""", baseUrl, preheaderText: $"Du er inviteret til {groupName} på Mini Møder");
	}

	public static string ChatNotification(string baseUrl, string recipientDisplayName, string senderDisplayName, string chatLink)
	{
		var encodedSenderName = WebUtility.HtmlEncode(senderDisplayName);
		return EmailTemplate.Wrap($"""
			{EmailConstants.Greeting(recipientDisplayName)}

			<p>Du har fået en ny besked fra <b>{encodedSenderName}</b></p>

			<p>Hvis du vil gå direkte til beskeden, kan du klikke på knappen nedenfor:</p>

			{EmailTemplate.Button(chatLink, "Læs besked")}
			""", baseUrl, preheaderText: $"Ny besked fra {senderDisplayName}");
	}

	public static string DeleteUser(string baseUrl, string deletionLink) =>
		EmailTemplate.Wrap($"""
			<p>Hej,</p>

			<p>Du har anmodet om at slette din bruger hos Mini Møder. Hvis du fortsætter, vil alle dine data blive permanent slettet og kan ikke genoprettes.</p>

			<p>Hvis du er sikker på, at du vil slette din bruger, skal du klikke på knappen nedenfor:</p>

			{EmailTemplate.Button(deletionLink, "Bekræft sletning", backgroundColor: "#a94442")}

			<p>Hvis du ikke anmodede om at slette din bruger, kan du ignorere denne e-mail.</p>
			""", baseUrl, preheaderText: "Anmodning om sletning af bruger");

	public static string GroupPostNotification(string baseUrl, string authorName, string postPreview, string groupUrl)
	{
		var encodedAuthorName = WebUtility.HtmlEncode(authorName);
		var encodedPostPreview = WebUtility.HtmlEncode(postPreview);
		encodedPostPreview = encodedPostPreview.Replace("\r\n", "<br/>").Replace("\n", "<br/>");

		return EmailTemplate.Wrap($"""
			<h4>Nyt opslag i din gruppe</h4>

			<p><b>{encodedAuthorName}</b> har oprettet et nyt opslag:</p>

			<blockquote style="border-left: 3px solid #dbab45; padding: 10px 15px; color: #41556b; background-color: #fdf8ee; margin: 16px 0;">
				{encodedPostPreview}
			</blockquote>

			{EmailTemplate.Button(groupUrl, "Se opslaget")}
			""", baseUrl, preheaderText: $"Nyt opslag af {authorName}");
	}

	public static string MembershipRequest(string baseUrl, string groupName)
	{
		var groupMembershipUrl = $"{baseUrl}/groups/{Uri.EscapeDataString(groupName)}/members";
		var encodedGroupName = WebUtility.HtmlEncode(groupName);
		return EmailTemplate.Wrap($"""
			<h4>Din gruppe <b>{encodedGroupName}</b> har modtaget en ny medlemskabsanmodning</h4>

			{EmailTemplate.Button(groupMembershipUrl, "Se anmodningen")}
			""", baseUrl, preheaderText: $"Ny medlemskabsanmodning til {groupName}");
	}

	public static string PartnerContactForm(string baseUrl, string? companyName, string contactPersonName, string email, string? phoneNumber, string message)
	{
		var companyInfo = !string.IsNullOrWhiteSpace(companyName)
			? $"<p><strong>Firma:</strong> {WebUtility.HtmlEncode(companyName)}</p>"
			: "";

		var phoneInfo = !string.IsNullOrWhiteSpace(phoneNumber)
			? $"<p><strong>Telefon:</strong> {WebUtility.HtmlEncode(phoneNumber)}</p>"
			: "";

		return EmailTemplate.Wrap($"""
			<h4>Partner henvendelse</h4>

			{companyInfo}
			<p><strong>Kontaktperson:</strong> {WebUtility.HtmlEncode(contactPersonName)}</p>
			<p><strong>Email:</strong> {WebUtility.HtmlEncode(email)}</p>
			{phoneInfo}

			<h5>Besked:</h5>
			<p>{WebUtility.HtmlEncode(message)}</p>
			""", baseUrl);
	}

	public static string PartnerImageApproval(string baseUrl, string partnerName, Guid partnerId, List<string> changes)
	{
		var approvalUrl = $"{baseUrl}/backoffice/partners/{partnerId}";
		var encodedPartnerName = WebUtility.HtmlEncode(partnerName);
		var listItems = string.Join("\n", changes.Select(c => $"<li>{WebUtility.HtmlEncode(c)}</li>"));

		return EmailTemplate.Wrap($"""
			<h4>Partner <b>{encodedPartnerName}</b> har uploadet nye ændringer til godkendelse</h4>

			<p>Ændringer:</p>
			<ul>
			{listItems}
			</ul>

			{EmailTemplate.Button(approvalUrl, "Godkend ændringer")}
			""", baseUrl);
	}

	public static string PartnerWelcome(string baseUrl, string partnerName, string email, string temporaryPassword)
	{
		var loginUrl = $"{baseUrl}/Account/Login";
		var encodedPartnerName = WebUtility.HtmlEncode(partnerName);
		var encodedEmail = WebUtility.HtmlEncode(email);
		var encodedTemporaryPassword = WebUtility.HtmlEncode(temporaryPassword);

		return EmailTemplate.Wrap($"""
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
			""", baseUrl, preheaderText: "Velkommen som partner på Mini Møder");
	}
}
