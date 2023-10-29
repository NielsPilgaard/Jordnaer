using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Shared;

[Index(nameof(DisplayName))]
public class Group
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }

    [Url]
    public string? ProfilePictureUrl { get; set; }

    [MaxLength(500, ErrorMessage = "Adresse må højest være 500 karakterer langt.")]
    public string? Address { get; set; }

    [DanishZipCode(ErrorMessage = "Post nummer skal være mellem 1000 og 9999")]
    public int? ZipCode { get; set; }

    [MaxLength(100, ErrorMessage = "By må højest være 50 karakterer langt.")]
    public string? City { get; set; }

    [MaxLength(128, ErrorMessage = "Gruppens navn må maks være 128 karakterer lang.")]
    public required string DisplayName { get; set; }

    [MaxLength(200, ErrorMessage = "Gruppens beskrivelse må maks være 200 karakterer lang.")]
    public string? ShortDescription { get; set; }

    [MaxLength(4000, ErrorMessage = "Gruppens beskrivelse må maks være 4000 karakterer lang.")]
    public string? Description { get; set; }

    public List<UserProfile> Members { get; set; } = new();
    public List<GroupMembership> Memberships { get; set; } = new();
    public List<Category> Categories { get; set; } = new();

    public int MemberCount { get; set; } = 0;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
