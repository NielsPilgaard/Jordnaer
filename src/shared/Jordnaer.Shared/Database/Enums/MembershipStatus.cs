using System.ComponentModel.DataAnnotations;
using NetEscapades.EnumGenerators;

namespace Jordnaer.Shared;

[EnumExtensions]
public enum MembershipStatus
{
	[Display(Name = "Aktivt")]
	Active = 0,

	[Display(Name = "Afventer svar fra gruppen")]
	PendingApprovalFromGroup = 1,

	[Display(Name = "Afventer svar fra brugeren")]
	PendingApprovalFromUser = 2,

	[Display(Name = "Afvist")]
	Rejected = 3,

	[Display(Name = "Forladt")]
	Left = 4
}
