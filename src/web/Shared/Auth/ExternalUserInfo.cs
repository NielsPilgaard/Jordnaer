using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared;

public class ExternalUserInfo
{
    [Required]
    public required string Email { get; set; }

    [Required]
    public required string ProviderKey { get; set; }
}
