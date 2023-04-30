using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared;

public class UserProfileDto
{
    [Required]
    public required string Id { get; set; }

    [MaxLength(100, ErrorMessage = "Fornavn skal være under 100 karakterer langt.")]
    public string? FirstName { get; set; }

    [MaxLength(250, ErrorMessage = "Efternavn skal være under 250 karakterer langt.")]
    public string? LastName { get; set; }

    [Phone(ErrorMessage = "Telefon nummeret må kun indeholde tal, mellemrum og +")]
    public string? PhoneNumber { get; set; }

    [MaxLength(500, ErrorMessage = "Adresse skal være under 500 karakterer langt.")]
    public string? Address { get; set; }

    [MaxLength(50, ErrorMessage = "Post nummer skal være under 50 karakterer langt.")]
    public string? ZipCode { get; set; }

    [MaxLength(100, ErrorMessage = "By skal være under 50 karakterer langt.")]
    public string? City { get; set; }

    [MaxLength(2000, ErrorMessage = "Beskrivelse skal være under 2000 karakterer langt.")]
    public string? Description { get; set; }

    public List<LookingFor> LookingFor { get; set; } = new();

    public List<ChildProfile> ChildProfiles { get; set; } = new();

    public DateTime? DateOfBirth { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
