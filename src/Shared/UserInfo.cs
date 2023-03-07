using System.ComponentModel.DataAnnotations;

namespace RemindMeApp.Shared;

public class UserInfo
{
    [Required]
    public string Username { get; set; } = default!;

    [Required]
    public string Password { get; set; } = default!;
}