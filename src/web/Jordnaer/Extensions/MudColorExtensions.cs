using MudBlazor.Utilities;

namespace Jordnaer.Extensions;

public static class MudColorExtensions
{
	public static string ToBackgroundColor(this MudColor color) => $"background-color: {color}";

	public static string ToTextColor(this MudColor color) => $"color: {color}";
}
