using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jordnaer.Shared.Extensions;
public static class EnumExtensions
{
	public static DisplayAttribute? GetDisplayAttribute(this Enum enumValue)
	{
		return enumValue.GetType()
						.GetField(enumValue.ToString())?.GetCustomAttribute<DisplayAttribute>();
	}
	
	public static string GetDisplayName(this Enum enumValue)
	{
		return enumValue.GetDisplayAttribute()?.Name ?? enumValue.ToString();
	}
}
