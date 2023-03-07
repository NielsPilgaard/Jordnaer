using System.ComponentModel.DataAnnotations;

namespace RemindMeApp.Shared;

public class ExternalUserInfo
{
    [Required]
    public string Username { get; set; } = default!;

    [Required]
    public string ProviderKey { get; set; } = default!;
}