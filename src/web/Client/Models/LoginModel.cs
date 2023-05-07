using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Client.Models;

public class LoginModel
{
    [Required(ErrorMessage = "Email er påkrævet")]
    [EmailAddress(ErrorMessage = "Skal være en gyldig email adresse")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Kodeord er påkrævet")]
    [StringLength(32, MinimumLength = 6, ErrorMessage = "Kodeordet skal være mellem 6 og 32 karakterer langt")]
    [RegularExpression("^(?=.*\\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[a-zA-Z]).*$",
        MatchTimeoutInMilliseconds = 1000,
        ErrorMessage = "Kodeordet skal indeholde mindst et småt bogstav, et stort bogstav og et tal")]
    public string? Password { get; set; }
}
