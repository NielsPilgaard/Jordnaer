namespace Jordnaer.Shared;

public class GroupSearchResult
{
    public List<GroupDto> Groups { get; set; } = new();
    public int TotalCount { get; set; }
}
