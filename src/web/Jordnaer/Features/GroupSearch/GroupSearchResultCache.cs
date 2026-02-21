using Jordnaer.Shared;

namespace Jordnaer.Features.GroupSearch;

public class GroupSearchResultCache
{
	public GroupSearchFilter? SearchFilter { get; set; }
	public GroupSearchResult? SearchResult { get; set; }
	public MapState? MapState { get; set; }
}

public class MapState
{
	public double ViewLatitude { get; set; }
	public double ViewLongitude { get; set; }
	public int Zoom { get; set; }
}