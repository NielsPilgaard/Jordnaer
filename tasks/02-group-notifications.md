# Task 02: Group Notifications

## Context

**App:** Jordnaer (.NET Blazor Server)
**Area:** Groups & Notifications
**Priority:** High
**Related:** Task 03 (Notification Settings) - users can opt out of group post emails

## Objective

Improve group management by making pending membership requests visible to admins/owners and sending email notifications to group members when new posts are created.

## Current State

- Membership requests create `PendingApprovalFromGroup` status in [GroupMembership.cs](src/shared/Jordnaer.Shared/Database/GroupMembership.cs)
- Admins receive email on membership request via [MembershipService.cs](src/web/Jordnaer/Features/Membership/MembershipService.cs)
- Group posts exist via [GroupPost.cs](src/shared/Jordnaer.Shared/Database/GroupPost.cs) and [GroupPostService.cs](src/web/Jordnaer/Features/GroupPosts/GroupPostService.cs)
- No visibility indicator for pending requests on group pages
- No email sent when new group posts are created
- Email infrastructure exists via MassTransit pattern in [SendEmailConsumer.cs](src/web/Jordnaer/Consumers/SendEmailConsumer.cs)

## Requirements

### 1. Pending Requests Indicator

Display pending membership request count for admins/owners:

- **Group card/list view:** Show badge with pending count on groups the user administrates
- **Group detail page:** Show prominent "X pending requests" alert/banner
- **Quick action:** Link directly to `/groups/{GroupName}/members` from the indicator
- Only visible to users with `PermissionLevel.Admin` or `OwnershipLevel.Owner`

### 2. New Post Email Notifications

When a new group post is created:

- Query all active group members (`MembershipStatus.Active`)
- Exclude the post author from notifications
- Respect notification preferences (see Task 03)
- Send email using existing MassTransit pattern

**Email content:**
- Subject: "Nyt opslag i {GroupName}"
- Body: Post author name, post preview (first 200 chars), link to group

### 3. Service Methods

Extend [GroupService.cs](src/web/Jordnaer/Features/Groups/GroupService.cs):
```csharp
Task<int> GetPendingMembershipCountAsync(Guid groupId, CancellationToken ct = default);
Task<Dictionary<Guid, int>> GetPendingMembershipCountsForUserAsync(CancellationToken ct = default);
```

Create new notification service or extend [GroupPostService.cs](src/web/Jordnaer/Features/GroupPosts/GroupPostService.cs):
```csharp
Task NotifyMembersOfNewPostAsync(GroupPost post, CancellationToken ct = default);
```

### 4. Integration Points

- Modify [GroupPostService.CreatePostAsync](src/web/Jordnaer/Features/GroupPosts/GroupPostService.cs) to trigger notifications
- Update group list/card components to show pending count badge
- Update group detail page to show pending requests alert

## Acceptance Criteria

### Pending Requests Visibility
- [ ] Admins/owners see pending request count on group cards
- [ ] Group detail page shows alert when pending requests exist
- [ ] Clicking indicator navigates to members page
- [ ] Count updates when requests are approved/rejected

### New Post Notifications
- [ ] Email sent to all active members when post created
- [ ] Post author excluded from notification
- [ ] Email contains group name, author, preview, and link
- [ ] Uses existing SendEmail/MassTransit infrastructure
- [ ] Respects user notification preferences (Task 03)

### Performance
- [ ] Pending counts fetched efficiently (single query for user's groups)
- [ ] Email notifications sent asynchronously (non-blocking)

## Files to Create/Modify

**Modify:**
- [GroupService.cs](src/web/Jordnaer/Features/Groups/GroupService.cs) - Add pending count methods
- [GroupPostService.cs](src/web/Jordnaer/Features/GroupPosts/GroupPostService.cs) - Trigger notifications on post create
- [EmailService.cs](src/web/Jordnaer/Features/Email/EmailService.cs) - Add group post notification method
- Group card/list components - Add pending badge
- Group detail page - Add pending alert

## Technical Notes

- Use `IPublishEndpoint` to publish `SendEmail` messages (non-blocking)
- Follow email template pattern from [ChatNotificationService.cs](src/web/Jordnaer/Features/Chat/ChatNotificationService.cs)
- Consider batch email optimization for large groups
- Pending count query should filter by `MembershipStatus.PendingApprovalFromGroup`
