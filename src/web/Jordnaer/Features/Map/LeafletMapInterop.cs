using Microsoft.JSInterop;
using MudBlazor;

namespace Jordnaer.Features.Map;

/// <summary>
/// Service for interacting with Leaflet.js maps via JavaScript interop
/// </summary>
public interface ILeafletMapInterop
{
	/// <summary>
	/// Initializes a Leaflet map instance
	/// </summary>
	Task<bool> InitializeMapAsync(string mapId, double lat, double lng, int zoom);

	/// <summary>
	/// Sets up a click handler on the map that calls back to C#
	/// </summary>
	Task<bool> SetupClickHandlerAsync<T>(string mapId, DotNetObjectReference<T> dotNetHelper) where T : class;

	/// <summary>
	/// Updates or creates a circle to show the search radius
	/// </summary>
	Task<bool> UpdateSearchRadiusAsync(string mapId, double lat, double lng, int radiusKm);

	/// <summary>
	/// Centers the map on a specific location
	/// </summary>
	Task<bool> CenterMapAsync(string mapId, double lat, double lng, int? zoom = null);

	/// <summary>
	/// Adds or updates a marker at the search location
	/// </summary>
	Task<bool> UpdateMarkerAsync(string mapId, double lat, double lng);

	/// <summary>
	/// Removes the search marker
	/// </summary>
	Task<bool> RemoveMarkerAsync(string mapId);

	/// <summary>
	/// Removes the search radius circle
	/// </summary>
	Task<bool> RemoveSearchRadiusAsync(string mapId);

	/// <summary>
	/// Disposes of a map instance
	/// </summary>
	Task<bool> DisposeMapAsync(string mapId);

	/// <summary>
	/// Updates group markers on the map with clustering support
	/// </summary>
	Task<bool> UpdateGroupMarkersAsync(string mapId, IEnumerable<GroupMarkerData> groups);

	/// <summary>
	/// Clears all group markers from the map
	/// </summary>
	Task<bool> ClearGroupMarkersAsync(string mapId);

	/// <summary>
	/// Fits the map view to show all group markers
	/// </summary>
	Task<bool> FitBoundsToMarkersAsync(string mapId, int padding = 50);
}

/// <summary>
/// Data transfer object for group marker information
/// </summary>
public record GroupMarkerData
{
	public required Guid Id { get; init; }
	public required string Name { get; init; }
	public string? ProfilePictureUrl { get; init; }
	public string? WebsiteUrl { get; init; }
	public string? ShortDescription { get; init; }
	public int? ZipCode { get; init; }
	public string? City { get; init; }
	public required double Latitude { get; init; }
	public required double Longitude { get; init; }
}

public class LeafletMapInterop(IJSRuntime jsRuntime) : ILeafletMapInterop
{
	private readonly IJSRuntime _jsRuntime = jsRuntime;

	public async Task<bool> InitializeMapAsync(string mapId, double lat, double lng, int zoom)
	{
		return await _jsRuntime.InvokeVoidAsyncWithErrorHandling(
			"leafletInterop.initializeMap", mapId, lat, lng, zoom);
	}

	public async Task<bool> SetupClickHandlerAsync<T>(string mapId, DotNetObjectReference<T> dotNetHelper) where T : class
	{
		return await _jsRuntime.InvokeVoidAsyncWithErrorHandling(
			"leafletInterop.setupClickHandler", mapId, dotNetHelper);
	}

	public async Task<bool> UpdateSearchRadiusAsync(string mapId, double lat, double lng, int radiusKm)
	{
		return await _jsRuntime.InvokeVoidAsyncWithErrorHandling(
			"leafletInterop.updateSearchRadius", mapId, lat, lng, radiusKm);
	}

	public async Task<bool> CenterMapAsync(string mapId, double lat, double lng, int? zoom = null)
	{
		return zoom.HasValue
			? await _jsRuntime.InvokeVoidAsyncWithErrorHandling(
				"leafletInterop.centerMap", mapId, lat, lng, zoom.Value)
			: await _jsRuntime.InvokeVoidAsyncWithErrorHandling(
			"leafletInterop.centerMap", mapId, lat, lng, null);
	}

	public async Task<bool> UpdateMarkerAsync(string mapId, double lat, double lng)
	{
		return await _jsRuntime.InvokeVoidAsyncWithErrorHandling(
			"leafletInterop.updateMarker", mapId, lat, lng);
	}

	public async Task<bool> RemoveMarkerAsync(string mapId)
	{
		return await _jsRuntime.InvokeVoidAsyncWithErrorHandling(
			"leafletInterop.removeMarker", mapId);
	}

	public async Task<bool> RemoveSearchRadiusAsync(string mapId)
	{
		return await _jsRuntime.InvokeVoidAsyncWithErrorHandling(
			"leafletInterop.removeSearchRadius", mapId);
	}

	public async Task<bool> DisposeMapAsync(string mapId)
	{
		return await _jsRuntime.InvokeVoidAsyncWithErrorHandling(
			"leafletInterop.disposeMap", mapId);
	}

	public async Task<bool> UpdateGroupMarkersAsync(string mapId, IEnumerable<GroupMarkerData> groups)
	{
		var materialized = groups?.ToList() ?? [];
		return await _jsRuntime.InvokeVoidAsyncWithErrorHandling(
			"leafletInterop.updateGroupMarkers", mapId, materialized);
	}

	public async Task<bool> ClearGroupMarkersAsync(string mapId)
	{
		return await _jsRuntime.InvokeVoidAsyncWithErrorHandling(
			"leafletInterop.clearGroupMarkers", mapId);
	}

	public async Task<bool> FitBoundsToMarkersAsync(string mapId, int padding = 50)
	{
		return await _jsRuntime.InvokeVoidAsyncWithErrorHandling(
			"leafletInterop.fitBoundsToMarkers", mapId, padding);
	}
}
