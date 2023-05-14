namespace Jordnaer.Shared.UserSearch;

public class UserSearchResult
{
    public List<UserDto> Users { get; set; } = new();
    public int TotalCount { get; set; }
}