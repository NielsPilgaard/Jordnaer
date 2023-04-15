using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared;

public class ExternalUserInfo
{
    [Required]
    public required string Username { get; set; }

    [Required]
    public required string ProviderKey { get; set; }
}
