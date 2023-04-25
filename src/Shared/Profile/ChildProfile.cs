using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jordnaer.Shared;

public class ChildProfile
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [ForeignKey(nameof(UserProfile))]
    public required string UserProfileId { get; set; }

    public virtual UserProfile UserProfile { get; set; } = default!;

    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }

    [MaxLength(250)]
    public string? LastName { get; set; }

    [Required]
    public required Gender Gender { get; set; }

    public DateTime DateOfBirth { get; set; }

    [MaxLength(4000)]
    public string? Interests { get; set; }

    public string? PictureUrl { get; set; }

    public int GetAge() => DateOfBirth.GetAge();
}
