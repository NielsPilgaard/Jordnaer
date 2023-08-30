namespace Jordnaer.Shared;

public class CurrentUserDto
{
    public string? Name { get; set; }
    public string? AuthenticationSchema { get; set; }
    public IEnumerable<Claim> Claims { get; set; } = Enumerable.Empty<Claim>();
}
