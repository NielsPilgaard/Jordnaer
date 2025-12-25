namespace Jordnaer.Shared;

public class PostSearchResult
{
	public List<PostDto> Posts { get; set; } = [];
	public int TotalCount { get; set; }
	public string? NextCursor { get; set; }

	/// <summary>
	/// Indicates whether there are more results available.
	/// This is a computed property based on NextCursor.
	/// </summary>
	public bool HasMore => NextCursor is not null;
}
