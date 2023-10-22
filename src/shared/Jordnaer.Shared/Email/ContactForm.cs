using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared;
public class ContactForm
{
    public string? Name { get; set; } = null!;

    [Required(ErrorMessage = "Email er påkrævet.")]
    [EmailAddress(ErrorMessage = "Email skal være gyldig.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Besked kan ikke være blank.")]
    public string Message { get; set; } = null!;
}
