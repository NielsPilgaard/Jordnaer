using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared;

public class ExternalUserInfo
{
    [Required]
    public string Username { get; set; } = default!;

    [Required]
    public string ProviderKey { get; set; } = default!;
}