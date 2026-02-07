namespace Jordnaer.Features.Email;

public static class EmailConstants
{
	public static readonly EmailRecipient ContactEmail = new() { Email = "kontakt@mini-moeder.dk", DisplayName = "Kontakt @ Mini Møder" };

	internal static string Greeting(string? userName) => userName is null
															 ? "<h4>Hej,</h4>"
															 : $"<h4>Hej {userName},</h4>";
}
