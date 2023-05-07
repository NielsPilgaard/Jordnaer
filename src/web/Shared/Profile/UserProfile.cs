using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Shared;

[Index(nameof(ZipCode), nameof(City))]
[Index(nameof(UserName))]
public class UserProfile
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required string Id { get; set; }

    [MaxLength(100, ErrorMessage = "Brugernavn må højest være 100 karakterer langt.")]
    [MinLength(3, ErrorMessage = "Brugernavn skal være mindst 3 karakterer langt.")]
    [RegularExpression("^[a-zA-Z0-9-_]+$",
        ErrorMessage = "Brugernavn må kun bestå af bogstaver, tal, bindestreg og understreg.")]
    public string? UserName { get; set; }

    [MaxLength(100, ErrorMessage = "Fornavn må højest være 100 karakterer langt.")]
    public string? FirstName { get; set; }

    [MaxLength(250, ErrorMessage = "Efternavn må højest være 250 karakterer langt.")]
    public string? LastName { get; set; }

    [Phone(ErrorMessage = "Telefon nummeret må kun indeholde tal, mellemrum og +")]
    public string? PhoneNumber { get; set; }

    [MaxLength(500, ErrorMessage = "Adresse må højest være 500 karakterer langt.")]
    public string? Address { get; set; }

    [MaxLength(50, ErrorMessage = "Post nummer må højest være 50 karakterer langt.")]
    public string? ZipCode { get; set; }

    [MaxLength(100, ErrorMessage = "By må højest være 50 karakterer langt.")]
    public string? City { get; set; }

    [MaxLength(2000, ErrorMessage = "Beskrivelse må højest være 2000 karakterer langt.")]
    public string? Description { get; set; }

    public List<LookingFor> LookingFor { get; set; } = new();

    public List<ChildProfile> ChildProfiles { get; set; } = new();

    public List<UserProfile> Contacts { get; set; } = new();

    public DateTime? DateOfBirth { get; set; }

    public string ProfilePictureUrl { get; set; } = ProfileConstants.DEFAULT_PROFILE_PICTURE;

    public int? GetAge() => DateOfBirth.GetAge();

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
