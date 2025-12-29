# Task 06: Invite User to Join Group

## Context

**App:** Jordnaer (.NET Blazor Server)
**Area:** Groups & Membership
**Priority:** Medium

## Objective

Allow group admins/owners to invite users to join their group, improving group discovery and member acquisition.

## Current State

- Users request membership via [MembershipService.cs](src/web/Jordnaer/Features/Membership/MembershipService.cs)
- `MembershipStatus.PendingApprovalFromUser` exists but unused
- Admin can view members at `/groups/{GroupName}/members`
- No invite functionality exists
- Email infrastructure available via MassTransit pattern

## Requirements

### 1. Invite User Flow

**Admin initiates invite:**
- Search for users by name/username from group detail or members page
- Select user(s) to invite
- Create `GroupMembership` with `MembershipStatus.PendingApprovalFromUser`

**User receives invite:**
- Email notification with group info and accept/decline links
- In-app notification (if notification system exists)
- View pending invites on profile or dedicated page

**User responds:**
- Accept: Status changes to `MembershipStatus.Active`
- Decline: Membership record deleted or marked declined

### 2. Service Methods

Extend [MembershipService.cs](src/web/Jordnaer/Features/Membership/MembershipService.cs):

```csharp
Task<OneOf<Success, UserAlreadyMember, Error>> InviteUserToGroupAsync(Guid groupId, string userId, CancellationToken ct = default);
Task<OneOf<Success, NotFound>> AcceptGroupInviteAsync(Guid groupId, CancellationToken ct = default);
Task<OneOf<Success, NotFound>> DeclineGroupInviteAsync(Guid groupId, CancellationToken ct = default);
Task<List<GroupInviteDto>> GetPendingInvitesForUserAsync(CancellationToken ct = default);
```

### 3. UI Components

**Invite Dialog:**
- User search with autocomplete
- Show user avatar, name, location
- Prevent inviting existing members or users with pending requests

**Pending Invites Display:**
- List on user profile or `/invites` page
- Show group name, description, who invited
- Accept/Decline buttons

### 4. Email Notification

Send invite email via existing MassTransit pattern:

- Subject: "Du er inviteret til {GroupName}"
- Body: Group name, description, inviter name, accept/decline links

## Acceptance Criteria

- [ ] Admins/owners can search and invite users from group page
- [ ] Cannot invite existing members or users with pending status
- [ ] Invited user receives email notification
- [ ] User can view pending invites
- [ ] User can accept invite (becomes active member)
- [ ] User can decline invite (record removed)
- [ ] Only admins/owners can send invites

## Files to Create/Modify

**Create:**
- `Components/InviteUserDialog.razor` - User search and invite dialog
- `Pages/Profile/Invites.razor` - View pending invites (optional dedicated page)

**Modify:**
- [MembershipService.cs](src/web/Jordnaer/Features/Membership/MembershipService.cs) - Add invite methods
- [GroupDetails.razor](src/web/Jordnaer/Pages/Groups/GroupDetails.razor) - Add invite button for admins
- [GroupMemberListComponent.razor](src/web/Jordnaer/Features/Groups/GroupMemberListComponent.razor) - Add invite action
- [EmailService.cs](src/web/Jordnaer/Features/Email/EmailService.cs) - Add invite email method

## Technical Notes

- Use `MembershipStatus.PendingApprovalFromUser` to distinguish from user-initiated requests
- Consider rate limiting invites to prevent spam
- Accept/decline links can use signed URLs or require authentication
- User search should exclude current members and pending invites
