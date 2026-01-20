using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared;

public class PartnerContactForm
{
	public string? CompanyName { get; set; }

	[Required(ErrorMessage = "Kontaktperson er påkrævet.")]
	public string ContactPersonName { get; set; } = null!;

	[Required(ErrorMessage = "Email er påkrævet.")]
	[EmailAddress(ErrorMessage = "Email skal være gyldig.")]
	public string Email { get; set; } = null!;

	public string? PhoneNumber { get; set; }

	[Required(ErrorMessage = "Beskeden må ikke være blank.")]
	[MinLength(1, ErrorMessage = "Beskeden må ikke være blank")]
	public string Message { get; set; } = null!;
}
