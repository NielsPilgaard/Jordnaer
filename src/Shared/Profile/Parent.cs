using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared;

public class Parent
{
    [Key]
    public Guid Id { get; set; }

    public required string ApplicationUserId { get; set; }

    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }

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

    [Required]
    public required LookingFor LookingFor { get; set; }

    public List<Child> Children { get; } = new();

    public List<Parent> Contacts { get; } = new();

    public DateTime? DateOfBirth { get; set; }

    public int? GetAge() => DateOfBirth.GetAge();
}
