using NetEscapades.EnumGenerators;
using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared;

[EnumExtensions]
public enum PermissionLevel
{
	[Display(Name = "Ingen")]
	None = 0,
	[Display(Name = "Medlem")]
	Write = 1,
	[Display(Name = "Administrator")]
	Admin = 2
}
