namespace Jordnaer.Shared;

public class UserDto
{
    public string? UserName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public int? ZipCode { get; set; }
    public string? City { get; set; }
    public required string ProfilePictureUrl { get; set; }
    public List<string> LookingFor { get; set; } = new();
    public List<ChildDto> Children { get; set; } = new();

    public override string ToString() => $"{FirstName} {LastName}";
}
