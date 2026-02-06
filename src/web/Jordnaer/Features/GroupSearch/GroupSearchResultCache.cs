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
	/// <summary>
	/// Marker position latitude (where the search is centered)
	/// </summary>
	public double MarkerLatitude { get; set; }

	/// <summary>
	/// Marker position longitude (where the search is centered)
	/// </summary>
	public double MarkerLongitude { get; set; }

	/// <summary>
	/// Map view center latitude (what the user is looking at)
	/// </summary>
	public double ViewLatitude { get; set; }

	/// <summary>
	/// Map view center longitude (what the user is looking at)
	/// </summary>
	public double ViewLongitude { get; set; }

	/// <summary>
	/// Current zoom level
	/// </summary>
	public int Zoom { get; set; }

	/// <summary>
	/// Address text if location was selected via address search
	/// </summary>
	public string? AddressText { get; set; }
}