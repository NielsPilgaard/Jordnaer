using System.ComponentModel.DataAnnotations;
using NetEscapades.EnumGenerators;

namespace Jordnaer.Shared;

[EnumExtensions]
public enum NotificationType
{
	[Display(Name = "Chatbesked")]
	ChatMessage = 0,

	[Display(Name = "Medlemskabsanmodning")]
	GroupMembershipRequest = 1,

	[Display(Name = "Gruppeinvitation")]
	GroupInvitation = 2,

	[Display(Name = "Medlemskab godkendt")]
	GroupMembershipApproved = 3,

	[Display(Name = "Medlemskab afvist")]
	GroupMembershipRejected = 4,

	[Display(Name = "Nyt opslag i gruppe")]
	GroupPost = 5,

	[Display(Name = "Ny bruger i nærheden")]
	NewUserNearby = 6,

	[Display(Name = "Systembesked")]
	System = 99
}
