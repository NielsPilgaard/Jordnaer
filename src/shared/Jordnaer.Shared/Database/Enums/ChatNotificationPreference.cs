using System.ComponentModel.DataAnnotations;
using NetEscapades.EnumGenerators;

namespace Jordnaer.Shared;

[EnumExtensions]
public enum ChatNotificationPreference
{
	[Display(Name = "Ingen")]
	None = 0,

	[Display(Name = "Kun første besked i ny samtale")]
	FirstMessageOnly = 1,

	[Display(Name = "Alle beskeder")]
	AllMessages = 2
}
