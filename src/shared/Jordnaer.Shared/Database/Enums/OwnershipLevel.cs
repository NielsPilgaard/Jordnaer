using System.ComponentModel.DataAnnotations;
using NetEscapades.EnumGenerators;

namespace Jordnaer.Shared;

[EnumExtensions]
public enum OwnershipLevel
{
	[Display(Name = "Ingen")]
	None = 0,
	[Display(Name = "Medlem")]
	Member = 1,
	[Display(Name = "Arver Ejerskab")]
	InheritsOwnership = 2,
	[Display(Name = "Ejer")]
	Owner = 3
}
