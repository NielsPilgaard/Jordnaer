using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared;

public class ExternalUserInfo
{
    [Required(ErrorMessage = "Påkrævet.")]
    public required string Email { get; set; }

    /// <summary>
    /// This is the user id we get from the external auth providers, Facebook, Google & Microsoft.
    /// <para>
    /// We use it as <c>UserProfile.Id</c> and <c>ApplicationUser.Id</c>.
    /// </para>
    /// </summary>
    [Required(ErrorMessage = "Påkrævet.")]
    public required string ProviderKey { get; set; }
}
