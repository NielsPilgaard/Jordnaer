using Microsoft.JSInterop;

namespace Jordnaer.Client;

public static class JsRuntimeExtensions
{
    public static async Task GoBackAsync(this IJSRuntime jsRuntime) => await jsRuntime.InvokeVoidAsync("history.back");
}
