using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Client.Models;

public class RegistrationModel
{
    [Required(ErrorMessage = "Fornavn er påkrævet")]
    [StringLength(30, ErrorMessage = "Fornavn kan ikke være længere end 30 bogstaver")]
    public string? Firstname { get; set; }

    [Required(ErrorMessage = "Efternavn er påkrævet")]
    [StringLength(100, ErrorMessage = "Efternavn kan ikke være længere end 100 bogstaver")]
    public string? Lastname { get; set; }

    [Required(ErrorMessage = "Email er påkrævet")]
    [EmailAddress(ErrorMessage = "Skal være en gyldig email adresse")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Adresse er påkrævet")]
    public string? Address { get; set; }

    [Required(ErrorMessage = "Post nr. er påkrævet")]
    public string? ZipCode { get; set; }

    [Required(ErrorMessage = "By er påkrævet")]
    public string? City { get; set; }

    [Required(ErrorMessage = "Kodeord er påkrævet")]
    [StringLength(32, MinimumLength = 6, ErrorMessage = "Kodeordet skal være mellem 6 og 32 karakterer langt")]
    [RegularExpression("^(?=.*\\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[a-zA-Z]).*$",
        MatchTimeoutInMilliseconds = 1000,
        ErrorMessage = "Kodeordet skal indeholde mindst et småt bogstav, et stort bogstav og et tal")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Gentag dit kodeord")]
    [Compare(nameof(Password))]
    public string? RepeatPassword { get; set; }
}
