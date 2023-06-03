using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Shared;

[Index(nameof(DateOfBirth))]
[Index(nameof(Gender))]
public class ChildProfile
{
    [Key]
    public Guid Id { get; set; }

    [ForeignKey(nameof(UserProfile))]
    public string UserProfileId { get; set; } = null!;

    [Required]
    [MaxLength(100, ErrorMessage = "Fornavn må højest være 100 karakterer langt.")]
    public required string FirstName { get; set; }

    [MaxLength(250, ErrorMessage = "Efternavn må højest være 250 karakterer langt.")]
    public string? LastName { get; set; }

    public Gender Gender { get; set; } = Gender.NotSet;

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(2000, ErrorMessage = "Beskrivelse må højest være 2000 karakterer langt.")]
    public string? Description { get; set; }

    public string PictureUrl { get; set; } = ProfileConstants.DEFAULT_PROFILE_PICTURE;

    public int? GetAge() => DateOfBirth.GetAge();

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
