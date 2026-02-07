using Jordnaer.Features.Email;
using Xunit;
using Xunit.Abstractions;

namespace Jordnaer.Tests.Email;

public class EmailPreviewTests(ITestOutputHelper output)
{
	private const string BaseUrl = "https://mini-moeder.dk";

	[Fact]
	public void Preview_ConfirmationEmail()
	{
		var body = $"""
				   {EmailConstants.Greeting("Anna")}

				   <p>Tak for at du registrerer dig hos Mini Møder.</p>
				   <p>Klik venligst på knappen nedenfor for at bekræfte din konto:</p>

				   {EmailTemplate.Button("https://mini-moeder.dk/confirm?code=abc123", "Bekræft din konto")}
				   """;

		var html = EmailTemplate.Wrap(body, BaseUrl, preheaderText: "Bekræft din Mini Møder konto");

		var path = EmailPreviewHelper.SavePreview(html, "confirmation");
		output.WriteLine($"Preview saved: {path}");
		output.WriteLine(html);

		Assert.Contains("Bekræft din konto", html);
		Assert.Contains("minimoeder_logo_payoff.png", html);
		Assert.Contains("Mini Møder Teamet", html);
	}

	[Fact]
	public void Preview_PasswordResetEmail()
	{
		var body = $"""
				   {EmailConstants.Greeting("Lars")}

				   <p>Vi har modtaget en anmodning om at nulstille din adgangskode.</p>
				   <p>Klik på knappen nedenfor for at indstille en ny adgangskode:</p>

				   {EmailTemplate.Button("https://mini-moeder.dk/reset?code=xyz789", "Nulstil adgangskode")}

				   <p>Hvis du ikke anmodede om at nulstille din adgangskode, bedes du ignorere denne e-mail.</p>
				   """;

		var html = EmailTemplate.Wrap(body, BaseUrl, preheaderText: "Nulstil din adgangskode");

		var path = EmailPreviewHelper.SavePreview(html, "password-reset");
		output.WriteLine($"Preview saved: {path}");

		Assert.Contains("Nulstil adgangskode", html);
		Assert.Contains("Mini Møder Teamet", html);
	}

	[Fact]
	public void Preview_GroupInviteEmail()
	{
		var body = $"""
				   <h4>Du er blevet inviteret til at blive medlem af gruppen <b>Forældre i København</b></h4>

				   {EmailTemplate.Button("https://mini-moeder.dk/groups/For%C3%A6ldre+i+K%C3%B8benhavn", "Se gruppen")}
				   """;

		var html = EmailTemplate.Wrap(body, BaseUrl, preheaderText: "Du er inviteret til Forældre i København");

		var path = EmailPreviewHelper.SavePreview(html, "group-invite");
		output.WriteLine($"Preview saved: {path}");

		Assert.Contains("Forældre i København", html);
		Assert.Contains("Se gruppen", html);
	}

	[Fact]
	public void Preview_ChatNotificationEmail()
	{
		var body = $"""
				   {EmailConstants.Greeting("Maria")}

				   <p>Du har fået en ny besked fra <b>Peter</b></p>

				   <p>Hvis du vil gå direkte til beskeden, kan du klikke på knappen nedenfor:</p>

				   {EmailTemplate.Button("https://mini-moeder.dk/chat/123", "Læs besked")}
				   """;

		var html = EmailTemplate.Wrap(body, BaseUrl, preheaderText: "Ny besked fra Peter");

		var path = EmailPreviewHelper.SavePreview(html, "chat-notification");
		output.WriteLine($"Preview saved: {path}");

		Assert.Contains("Læs besked", html);
		Assert.Contains("Peter", html);
	}

	[Fact]
	public void Preview_DeleteUserEmail()
	{
		var body = $"""
				   <p>Hej,</p>

				   <p>Du har anmodet om at slette din bruger hos Mini Møder. Hvis du fortsætter, vil alle dine data blive permanent slettet og kan ikke genoprettes.</p>

				   <p>Hvis du er sikker på, at du vil slette din bruger, skal du klikke på knappen nedenfor:</p>

				   {EmailTemplate.Button("https://mini-moeder.dk/delete-user/token123", "Bekræft sletning", backgroundColor: "#a94442")}

				   <p>Hvis du ikke anmodede om at slette din bruger, kan du ignorere denne e-mail.</p>
				   """;

		var html = EmailTemplate.Wrap(body, BaseUrl, preheaderText: "Anmodning om sletning af bruger");

		var path = EmailPreviewHelper.SavePreview(html, "delete-user");
		output.WriteLine($"Preview saved: {path}");

		Assert.Contains("Bekræft sletning", html);
		Assert.Contains("#a94442", html);
	}

	[Fact]
	public void Preview_GroupPostNotificationEmail()
	{
		var body = $"""
				   <h4>Nyt opslag i din gruppe</h4>

				   <p><b>Mette Hansen</b> har oprettet et nyt opslag:</p>

				   <blockquote style="border-left: 3px solid #dbab45; padding: 10px 15px; color: #41556b; background-color: #fdf8ee; margin: 16px 0;">
				       Hej alle! Er der nogen der har lyst til at mødes i parken i morgen eftermiddag? Vejret ser ud til at blive dejligt...
				   </blockquote>

				   {EmailTemplate.Button("https://mini-moeder.dk/groups/Legekammerater", "Se opslaget")}
				   """;

		var html = EmailTemplate.Wrap(body, BaseUrl, preheaderText: "Nyt opslag af Mette Hansen");

		var path = EmailPreviewHelper.SavePreview(html, "group-post-notification");
		output.WriteLine($"Preview saved: {path}");

		Assert.Contains("Mette Hansen", html);
		Assert.Contains("Se opslaget", html);
		Assert.Contains("blockquote", html);
	}

	[Fact]
	public void Preview_MembershipRequestEmail()
	{
		var body = $"""
				   <h4>Din gruppe <b>Legekammerater Østerbro</b> har modtaget en ny medlemskabsanmodning</h4>

				   {EmailTemplate.Button("https://mini-moeder.dk/groups/Legekammerater+%C3%98sterbro/members", "Se anmodningen")}
				   """;

		var html = EmailTemplate.Wrap(body, BaseUrl, preheaderText: "Ny medlemskabsanmodning til Legekammerater Østerbro");

		var path = EmailPreviewHelper.SavePreview(html, "membership-request");
		output.WriteLine($"Preview saved: {path}");

		Assert.Contains("Legekammerater Østerbro", html);
		Assert.Contains("Se anmodningen", html);
	}

	[Fact]
	public void Preview_PartnerWelcomeEmail()
	{
		var body = $"""
				   <h4>Velkommen som partner på Mini Møder, Legetøjsbutikken!</h4>

				   <p>Din partnerkonto er blevet oprettet. Du kan nu logge ind og administrere dine partner-annoncer.</p>

				   <h5>Login oplysninger:</h5>
				   <ul>
				       <li><strong>Email:</strong> partner@example.com</li>
				       <li><strong>Midlertidigt kodeord:</strong> <code>TempPass123!</code></li>
				   </ul>

				   <p><strong>VIGTIGT:</strong> Af sikkerhedsmæssige årsager bedes du ændre dit kodeord efter første login.</p>

				   <h5>Sådan kommer du i gang:</h5>
				   <ol>
				       <li>Log ind med dine oplysninger</li>
				       <li>Skift dit kodeord under <em>Profil i øverste højre hjørne → Kontoindstillinger → Adgangskode</em></li>
				       <li>Gå til dit partner dashboard for at uploade annoncer og se statistik</li>
				   </ol>

				   {EmailTemplate.Button("https://mini-moeder.dk/Account/Login", "Log ind")}
				   """;

		var html = EmailTemplate.Wrap(body, BaseUrl, preheaderText: "Velkommen som partner på Mini Møder");

		var path = EmailPreviewHelper.SavePreview(html, "partner-welcome");
		output.WriteLine($"Preview saved: {path}");

		Assert.Contains("Legetøjsbutikken", html);
		Assert.Contains("Log ind", html);
		Assert.Contains("TempPass123!", html);
	}
}
