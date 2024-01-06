using SendGrid.Helpers.Mail;

namespace Jordnaer.Features.Email;

public static class EmailConstants
{
	public static readonly EmailAddress ContactEmail =
		new("kontakt@mini-moeder.dk", "Kontakt @ Mini Møder");
}
