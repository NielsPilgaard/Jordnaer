using Microsoft.JSInterop;
using MudBlazor;

namespace Jordnaer.Extensions;

public static class JsRuntimeExtensions
{
	public static async Task GoBackAsync(this IJSRuntime jsRuntime)
		=> await jsRuntime.InvokeVoidAsyncWithErrorHandling("history.back");

	public static async Task NavigateTo(this IJSRuntime jsRuntime, string newUrl)
		=> await jsRuntime.InvokeVoidAsyncWithErrorHandling("utilities.updatePathAndQueryString", newUrl);

	public static async ValueTask<GeoLocation?> GetGeolocation(this IJSRuntime jsRuntime)
	{
		var (success, geoLocation) = await jsRuntime.InvokeAsyncWithErrorHandling<GeoLocation>("utilities.getGeolocation");

		return success
				   ? geoLocation
				   : null;
	}
}

public readonly record struct GeoLocation(float Latitude, float Longitude);
