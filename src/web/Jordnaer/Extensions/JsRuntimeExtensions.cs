using Microsoft.JSInterop;

namespace Jordnaer.Extensions;

public static class JsRuntimeExtensions
{
	public static async Task GoBackAsync(this IJSRuntime jsRuntime) => await jsRuntime.InvokeVoidAsync("history.back");
	public static async ValueTask<GeoLocation> GetGeolocation(this IJSRuntime jsRuntime) => await jsRuntime.InvokeAsync<GeoLocation>("getGeolocation");
}

public readonly record struct GeoLocation(long X, long Y);
