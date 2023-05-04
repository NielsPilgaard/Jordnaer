using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jordnaer.Shared;

public class ChildProfile
{
    [Key]
    public Guid Id { get; set; }

    [ForeignKey(nameof(UserProfile))]
    public string UserProfileId { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }

    [MaxLength(250)]
    public string? LastName { get; set; }

    [Required]
    public required Gender Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public string? PictureUrl { get; set; }

    public int? GetAge() => DateOfBirth.GetAge();

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
