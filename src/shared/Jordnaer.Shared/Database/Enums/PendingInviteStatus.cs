using System.ComponentModel.DataAnnotations;
using NetEscapades.EnumGenerators;

namespace Jordnaer.Shared;

[EnumExtensions]
public enum PendingInviteStatus
{
	[Display(Name = "Afventer")]
	Pending = 0,

	[Display(Name = "Accepteret")]
	Accepted = 1,

	[Display(Name = "Udløbet")]
	Expired = 2
}
