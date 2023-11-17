namespace Jordnaer.Shared;

public class GroupSearchResult
{
    public List<GroupSlim> Groups { get; set; } = new();
    public int TotalCount { get; set; }
}
