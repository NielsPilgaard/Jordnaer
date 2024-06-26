using SendGrid.Helpers.Mail;

namespace Jordnaer.Features.Email;

public static class EmailConstants
{
	public static readonly EmailAddress ContactEmail =
		new("kontakt@mini-moeder.dk", "Kontakt @ Mini Møder");

	public static readonly string Signature = """
									  <p>Venlig hilsen,<br />
									  <p>Mini Møder Teamet</p>
									  """;

	internal static string Greeting(string? userName) => userName is null
															 ? "<h4>Hej,</h4>"
															 : $"<h4>Hej {userName},</h4>";
}
