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
}
