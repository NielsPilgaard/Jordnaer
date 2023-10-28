using System.ComponentModel.DataAnnotations;
using NetEscapades.EnumGenerators;

namespace Jordnaer.Shared;

[EnumExtensions]
public enum MembershipStatus
{
    [Display(Name = "Aktivt")]
    Active = 0,

    [Display(Name = "Afventer svar")]
    PendingApprovalFromGroup = 1,

    [Display(Name = "Afventer svar")]
    PendingApprovalFromUser = 2,

    [Display(Name = "Afvist")]
    Rejected = 3
}
