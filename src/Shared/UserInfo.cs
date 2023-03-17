using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared;

public class UserInfo
{
    [Required]
    public required string Email { get; set; }

    [Required]
    public required string Password { get; set; }
}
