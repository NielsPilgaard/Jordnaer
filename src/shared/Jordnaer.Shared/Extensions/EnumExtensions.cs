using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Jordnaer.Shared.Extensions;
public static class EnumExtensions
{
	public static DisplayAttribute? GetDisplayAttribute(this Enum enumValue)
	{
		return enumValue.GetType()
						.GetField(enumValue.ToString())?
						.GetCustomAttribute<DisplayAttribute>();
	}

	public static string GetDisplayName(this Enum enumValue)
	{
		return enumValue.GetDisplayAttribute()?.Name ?? enumValue.ToString();
	}

	public static string? ToDisplayName<T>(this T enumValue) where T : Enum =>
		enumValue.GetDisplayAttribute()?
			.GetName();
}
