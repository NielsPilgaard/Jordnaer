# Task 05: Consolidate Notifications System

## Context

**App:** Jordnaer (.NET Blazor Server)
**Area:** Notifications & Real-time Updates
**Priority:** Medium
**Related:** Task 04 (User Subscriptions)

## Objective

Consolidate group access requests, unread chat messages, and other notification types into a unified Notifications system. All notifications should be delivered via SignalR with optional email delivery. The goal is to clean up scattered notification logic into a maintainable, extensible architecture.

## Current State

### SignalR Hubs (Separate)
- [ChatHub.cs](src/web/Jordnaer/Features/Chat/ChatHub.cs) - `/hubs/chat` for messages
- [GroupMembershipHub.cs](src/web/Jordnaer/Features/Groups/GroupMembershipHub.cs) - `/hubs/group-membership` for pending requests

### Notification Services (Scattered)
- [ChatNotificationService.cs](src/web/Jordnaer/Features/Chat/ChatNotificationService.cs) - Chat email notifications
- [GroupMembershipNotificationService.cs](src/web/Jordnaer/Features/Groups/GroupMembershipNotificationService.cs) - Group membership SignalR
- [EmailService.cs](src/web/Jordnaer/Features/Email/EmailService.cs) - Various email methods

### SignalR Clients (Multiple)
- [ChatSignalRClient.cs](src/web/Jordnaer/Features/Chat/ChatSignalRClient.cs)
- [UnreadMessageSignalRClient.cs](src/web/Jordnaer/Features/Chat/UnreadMessageSignalRClient.cs)
- [GroupMembershipSignalRClient.cs](src/web/Jordnaer/Features/Groups/GroupMembershipSignalRClient.cs)

### Data Models (Separate)
- [UnreadMessage.cs](src/shared/Jordnaer.Shared/Database/UnreadMessage.cs) - Tracks unread chat messages
- `GroupMembershipStatusChanged` event - Pending request count changes
- No unified notification model

## Requirements

### 1. Unified Notification Model

Create a notification entity that supports rich content:

```csharp
public class Notification
{
    public Guid Id { get; set; }
    public required string RecipientId { get; set; }

    // Content
    public required string Title { get; set; }
    public string? Description { get; set; }  // Supports **bold**, *italic*, basic markdown
    public string? ImageUrl { get; set; }     // Optional avatar/icon
    public string? LinkUrl { get; set; }      // Optional click-through URL

    // Metadata
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ReadUtc { get; set; }

    // Source reference (for deduplication/grouping)
    public string? SourceType { get; set; }   // "Chat", "GroupMembership", "GroupPost", etc.
    public string? SourceId { get; set; }     // ChatId, GroupId, etc.

    // Navigation
    public UserProfile Recipient { get; set; }
}

public enum NotificationType
{
    ChatMessage = 0,
    GroupMembershipRequest = 1,    // Pending request (for admins)
    GroupInvitation = 2,           // Invited to group (for users)
    GroupMembershipApproved = 3,   // Request approved
    GroupMembershipRejected = 4,   // Request rejected
    GroupPost = 5,                 // New post in group
    NewUserNearby = 6,             // From subscription system (Task 04)
    System = 99                    // System announcements
}
```

### 2. Unified NotificationHub

Create a single SignalR hub for all notification types:

```csharp
// Hub interface
public interface INotificationHub
{
    Task ReceiveNotification(NotificationDto notification);
    Task NotificationRead(Guid notificationId);
    Task NotificationsCleared(string sourceType, string? sourceId);
    Task UnreadCountChanged(int totalUnread);
}

// Hub route: /hubs/notifications
```

**Migration strategy:** Keep existing hubs working during transition, then deprecate.

### 3. NotificationService

Consolidate notification creation and delivery:

```csharp
public interface INotificationService
{
    // Create and send notification (SignalR + optional email)
    Task SendAsync(CreateNotificationRequest request, CancellationToken ct = default);

    // Batch send to multiple recipients
    Task SendToManyAsync(CreateNotificationRequest request, IEnumerable<string> recipientIds, CancellationToken ct = default);

    // Read management
    Task MarkAsReadAsync(string userId, Guid notificationId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(string userId, CancellationToken ct = default);
    Task MarkSourceAsReadAsync(string userId, string sourceType, string sourceId, CancellationToken ct = default);

    // Query
    Task<List<NotificationDto>> GetUnreadAsync(string userId, int limit = 50, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default);
}

public class CreateNotificationRequest
{
    public required string RecipientId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public NotificationType Type { get; set; }
    public string? SourceType { get; set; }
    public string? SourceId { get; set; }

    // Email options
    public bool SendEmail { get; set; }
    public string? EmailSubject { get; set; }  // Falls back to Title if null
}
```

### 4. Migrate Existing Notifications

#### Chat Messages
- Keep `UnreadMessage` table for chat-specific unread tracking (performance)
- Create notification on first unread message in a chat (not every message)
- Clear notification when chat is opened (mark as read)
- Email: Respect existing `ChatNotificationPreference`

**Example notification:**
```
Title: "Ny besked fra {SenderName}"
Description: "{MessagePreview}..."
ImageUrl: sender's profile picture
LinkUrl: "/chat/{chatId}"
SourceType: "Chat"
SourceId: "{chatId}"
```

#### Group Membership Requests (for admins)
- Create notification when user requests to join
- Clear when admin approves/rejects
- Email: Send to all admins/owners

**Example notification:**
```
Title: "{UserName} vil gerne være med i {GroupName}"
Description: null
ImageUrl: requester's profile picture
LinkUrl: "/groups/{groupId}/members"
SourceType: "GroupMembership"
SourceId: "{groupId}:{userId}"
```

#### Group Invitations (for users)
- Create notification when invited to group
- Clear when user accepts/rejects
- Email: Always send

**Example notification:**
```
Title: "Du er inviteret til {GroupName}"
Description: "Inviteret af {InviterName}"
ImageUrl: group image
LinkUrl: "/groups/{groupId}"
SourceType: "GroupInvitation"
SourceId: "{groupId}"
```

#### Group Membership Status Changes (for users)
- Notify when request is approved/rejected
- Email: Send approval confirmations

**Example notification (approved):**
```
Title: "Du er nu medlem af {GroupName}"
Description: null
ImageUrl: group image
LinkUrl: "/groups/{groupId}"
```

#### Group Posts
- Keep existing email-only behavior initially
- Optionally add in-app notification based on user preference

### 5. Notification UI Components

#### NotificationBell Component
- Display in header/navbar
- Show unread count badge
- Dropdown with recent notifications
- Link to full notifications page

```razor
@* Components/NotificationBell.razor *@
<div class="notification-bell">
    <button @onclick="ToggleDropdown">
        <Icon Name="bell" />
        @if (UnreadCount > 0)
        {
            <span class="badge">@(UnreadCount > 99 ? "99+" : UnreadCount)</span>
        }
    </button>

    @if (IsOpen)
    {
        <div class="notification-dropdown">
            @foreach (var notification in RecentNotifications)
            {
                <NotificationItem Notification="notification" OnClick="HandleClick" />
            }
            <a href="/notifications">Se alle</a>
        </div>
    }
</div>
```

#### NotificationItem Component
- Display notification with title, description, image
- Render markdown in description (bold, italic)
- Unread indicator
- Click navigates to LinkUrl and marks as read

#### Notifications Page
- Full list with pagination
- Filter by type
- Mark all as read
- Route: `/notifications`

### 6. Notification Preferences

Extend [NotificationSettingsService.cs](src/web/Jordnaer/Features/Notifications/NotificationSettingsService.cs):

```csharp
public class NotificationPreferences
{
    // Per-type email preferences
    public bool EmailOnChatMessage { get; set; } = true;      // Maps to ChatNotificationPreference
    public bool EmailOnGroupRequest { get; set; } = true;     // Admin: new membership requests
    public bool EmailOnGroupInvite { get; set; } = true;      // User: invited to group
    public bool EmailOnGroupApproval { get; set; } = true;    // User: request approved
    public bool EmailOnGroupPost { get; set; } = true;        // Existing per-group setting
    public bool EmailOnNewUserNearby { get; set; } = true;    // Task 04 subscriptions

    // Global settings
    public bool PushNotificationsEnabled { get; set; } = true;  // Future: mobile push
}
```

Settings UI at `/settings/notifications`.

### 7. Email Templates

Create consistent email templates for all notification types:

| Type | Subject | Template |
|------|---------|----------|
| ChatMessage | "Ny besked fra {Name}" | Message preview, link to chat |
| GroupMembershipRequest | "Ny anmodning i {Group}" | Requester info, approve/reject link |
| GroupInvitation | "Invitation til {Group}" | Group info, accept link |
| GroupApproved | "Velkommen til {Group}" | Confirmation, link to group |
| GroupPost | "Nyt opslag i {Group}" | Post preview, link |
| NewUserNearby | "Ny bruger i dit område" | User info, link to profile |

Use existing email infrastructure via [SendEmailConsumer.cs](src/web/Jordnaer/Consumers/SendEmailConsumer.cs).

## Implementation Phases

### Phase 1: Core Infrastructure
1. Create `Notification` entity and migration
2. Create `NotificationHub` and `INotificationHub`
3. Create `NotificationService` with basic CRUD
4. Create `NotificationSignalRClient` for Blazor

### Phase 2: UI Components
1. Create `NotificationBell` component
2. Create `NotificationItem` component
3. Create `/notifications` page
4. Integrate bell into main layout

### Phase 3: Migrate Chat Notifications
1. Update `SendMessageConsumer` to create notifications
2. Update `StartChatConsumer` to create notifications
3. Update chat UI to mark notifications as read
4. Keep `UnreadMessage` table for chat-specific counts

### Phase 4: Migrate Group Notifications
1. Update group membership flows to create notifications
2. Migrate `GroupMembershipNotificationService` logic
3. Update group UI to mark notifications as read

### Phase 5: Cleanup & Polish
1. Add notification preferences UI
2. Consolidate email templates
3. Deprecate old SignalR hubs (or keep as aliases)
4. Add notification grouping (e.g., "3 new messages from {Name}")

## Acceptance Criteria

### Core System
- [ ] `Notification` entity created with migration
- [ ] `NotificationHub` delivers real-time notifications
- [ ] `NotificationService` handles creation, delivery, and read tracking
- [ ] Notifications persist to database

### UI
- [ ] Notification bell shows in header with unread count
- [ ] Dropdown shows recent notifications
- [ ] Full notifications page with filtering
- [ ] Clicking notification navigates and marks as read
- [ ] Markdown rendering in descriptions (bold, italic)

### Chat Integration
- [ ] New chat creates notification for recipients
- [ ] New message in existing chat creates/updates notification
- [ ] Opening chat marks notifications as read
- [ ] Respects `ChatNotificationPreference` for emails

### Group Integration
- [ ] Membership requests create admin notifications
- [ ] Invitations create user notifications
- [ ] Approval/rejection creates user notifications
- [ ] Handling request clears admin notification

### Email
- [ ] All notification types can trigger emails
- [ ] Email preferences respected per type
- [ ] Consistent email templates

### Preferences
- [ ] Per-type email preferences in settings
- [ ] Existing preferences migrated

## Files to Create

| File | Purpose |
|------|---------|
| `src/shared/Jordnaer.Shared/Database/Notification.cs` | Entity |
| `src/shared/Jordnaer.Shared/Database/Enums/NotificationType.cs` | Enum |
| `src/shared/Jordnaer.Shared/Notifications/NotificationDto.cs` | DTO |
| `src/web/Jordnaer/Features/Notifications/NotificationHub.cs` | SignalR hub |
| `src/web/Jordnaer/Features/Notifications/INotificationHub.cs` | Hub interface |
| `src/web/Jordnaer/Features/Notifications/NotificationService.cs` | Main service |
| `src/web/Jordnaer/Features/Notifications/NotificationSignalRClient.cs` | Blazor client |
| `src/web/Jordnaer/Components/NotificationBell.razor` | Bell component |
| `src/web/Jordnaer/Components/NotificationItem.razor` | Item component |
| `src/web/Jordnaer/Pages/Notifications/NotificationsPage.razor` | Full page |
| `src/web/Jordnaer/Pages/Settings/NotificationSettings.razor` | Preferences |
| Database migration | Schema changes |

## Files to Modify

| File | Change |
|------|---------|
| [Program.cs](src/web/Jordnaer/Program.cs) | Register hub, map endpoint |
| [MainLayout.razor](src/web/Jordnaer/Components/MainLayout.razor) | Add NotificationBell |
| [SendMessageConsumer.cs](src/web/Jordnaer/Consumers/SendMessageConsumer.cs) | Create notification |
| [StartChatConsumer.cs](src/web/Jordnaer/Consumers/StartChatConsumer.cs) | Create notification |
| [GroupMembershipService.cs](src/web/Jordnaer/Features/Groups/GroupMembershipService.cs) | Create notifications |
| [NotificationSettingsService.cs](src/web/Jordnaer/Features/Notifications/NotificationSettingsService.cs) | Extended preferences |
| [JordnaerDbContext.cs](src/web/Jordnaer/Database/JordnaerDbContext.cs) | Add Notifications DbSet |

## Technical Notes

- **SignalR user targeting:** Use `Clients.User(userId)` - relies on `ClaimTypes.NameIdentifier`
- **Markdown rendering:** Use Markdig or similar for safe HTML rendering of bold/italic
- **Performance:** Index `Notification` on `(RecipientId, IsRead, CreatedUtc DESC)`
- **Cleanup:** Consider background job to delete old read notifications (>30 days)
- **Grouping:** Future enhancement - group similar notifications (e.g., multiple messages from same chat)
