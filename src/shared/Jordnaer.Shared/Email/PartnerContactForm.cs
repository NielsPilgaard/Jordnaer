using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared;

public class PartnerContactForm
{
	public string? CompanyName { get; set; }

	[Required(ErrorMessage = "Kontaktperson er påkrævet.", AllowEmptyStrings = false)]
	public string ContactPersonName { get; set; } = null!;

	[Required(ErrorMessage = "Email er påkrævet.")]
	[EmailAddress(ErrorMessage = "Email skal være gyldig.")]
	public string Email { get; set; } = null!;

	[Phone(ErrorMessage = "Telefonnummeret er ikke gyldigt.")]
	public string? PhoneNumber { get; set; }

	[Required(ErrorMessage = "Beskeden må ikke være blank.", AllowEmptyStrings = false)]
	public string Message { get; set; } = null!;
}
