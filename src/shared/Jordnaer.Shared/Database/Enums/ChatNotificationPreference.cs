using System.ComponentModel.DataAnnotations;
using NetEscapades.EnumGenerators;

namespace Jordnaer.Shared;

[EnumExtensions]
public enum ChatNotificationPreference
{
	[Display(Name = "Kun første besked i ny samtale")]
	FirstMessageOnly = 0,

	[Display(Name = "Ingen")]
	None = 1,

	[Display(Name = "Alle beskeder")]
	AllMessages = 2
}
