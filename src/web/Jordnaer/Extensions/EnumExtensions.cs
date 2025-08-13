using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Jordnaer.Extensions;

public static class EnumExtensions
{
	public static string? ToDisplayName<T>(this T enumValue) where T : Enum =>
		enumValue.GetType()
				 .GetMember(enumValue.ToString())
				 .First()
				 .GetCustomAttribute<DisplayAttribute>()?
				 .Name;
}