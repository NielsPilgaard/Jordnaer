using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared;

public class UserProfileDto
{
    public int Id { get; set; }

    public required string ApplicationUserId { get; set; }

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

    [MaxLength(2000)]
    public string? Interests { get; set; }

    public List<LookingFor> LookingFor { get; set; } = new();

    public List<ChildProfile> ChildProfiles { get; set; } = new();

    public DateTime? DateOfBirth { get; set; }

    public string? ProfilePictureUrl { get; set; }
}