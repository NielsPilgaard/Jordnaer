using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Shared;

[Index(nameof(ZipCode), nameof(City))]
[Index(nameof(FirstName), nameof(LastName))]
public class UserProfile
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required string Id { get; set; }

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(250)]
    public string? LastName { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? ZipCode { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public List<LookingFor> LookingFor { get; set; } = new();

    public List<ChildProfile> ChildProfiles { get; set; } = new();

    public List<UserProfile> Contacts { get; set; } = new();

    public DateTime? DateOfBirth { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public int? GetAge() => DateOfBirth.GetAge();

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
