using Jordnaer.Features.Authentication;
using Jordnaer.Shared;
using Jordnaer.SignalR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Jordnaer.Features.Groups;

public class GroupMembershipSignalRClient(
	CurrentUser currentUser,
	ILogger<AuthenticatedSignalRClientBase> logger,
	NavigationManager navigationManager)
	: AuthenticatedSignalRClientBase(logger, currentUser, navigationManager, "/hubs/group-membership")
{
	public void OnMembershipStatusChanged(Func<GroupMembershipStatusChanged, Task> action)
	{
		if (HubConnection is null)
		{
			return;
		}

		HubConnection.Remove(nameof(IGroupMembershipHub.MembershipStatusChanged));
		HubConnection.On(nameof(IGroupMembershipHub.MembershipStatusChanged), action);
	}
}
