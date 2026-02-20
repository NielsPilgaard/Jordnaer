# Task 05b: Notification System Cleanup

## Context

Task 05 created the unified notification system (Notification entity, NotificationHub, NotificationService, NotificationBell, NotificationsPage). However, the old notification infrastructure was left running in parallel. This task covers the remaining work to fully migrate and clean up.

## What's Done (Task 05)

- Notification entity, NotificationType enum, NotificationDto, CreateNotificationRequest
- NotificationHub at `/hubs/notifications` with NotificationSignalRClient
- NotificationService (create, send, mark read, query)
- NotificationBell component in TopBar with real-time updates
- NotificationsPage at `/notifications` with filtering and pagination
- NotificationItem component
- Consumers (SendMessageConsumer, StartChatConsumer) create in-app notifications
- MembershipService creates notifications for requests, invitations, accept/decline
- GroupService.UpdateMembership creates notifications on approval/rejection
- Notification email preferences on UserProfile (EmailOnGroupMembershipRequest, EmailOnGroupInvitation, EmailOnGroupMembershipResponse)
- Settings page updated with membership notification preferences
- EF migration for Notifications table
- Data migration script for UnreadMessages -> Notifications

## What Remains

### 1. Retire ChatNotificationService — Use NotificationService for Chat Emails

**Problem:** Both consumers still inject and call `ChatNotificationService` for email delivery. The new `NotificationService.SendAsync` is called alongside it but with `SendEmail` defaulting to `false`. Chat emails are handled exclusively by the old service.

**What to do:**

- In `SendMessageConsumer`: remove `ChatNotificationService` dependency. Instead, check `ChatNotificationPreference` per recipient before calling `notificationService.SendAsync`, and set `SendEmail = true` for recipients whose preference allows it (`AllMessages` for follow-up messages).
- In `StartChatConsumer`: same — check `ChatNotificationPreference` per recipient, set `SendEmail = true` for those with `FirstMessageOnly` or `AllMessages`.
- The preference check currently lives inside `ChatNotificationService.NotifyRecipients` / `NotifyRecipientsOfNewMessage`. Move that logic into the consumers or into a helper, then query `UserProfile.ChatNotificationPreference` via the DbContext that's already available.
- After migration: delete `ChatNotificationService.cs` and remove its DI registration from `Features/Chat/WebApplicationBuilderExtensions.cs`.

**Files:**

- Modify: `src/web/Jordnaer/Consumers/SendMessageConsumer.cs`
- Modify: `src/web/Jordnaer/Consumers/StartChatConsumer.cs`
- Delete: `src/web/Jordnaer/Features/Chat/ChatNotificationService.cs`
- Modify: `src/web/Jordnaer/Features/Chat/WebApplicationBuilderExtensions.cs` (remove registration)

### 2. Deduplicate Chat Notifications — Only Create on First Unread

**Problem:** Both consumers create a new `Notification` on **every** message. The spec says "Create notification on first unread message in a chat (not every message)." This means a user could get 50 notification rows for 50 messages in the same chat.

**What to do:**

- Before creating a chat notification in both consumers, check if an unread notification already exists for this recipient with `SourceType = "Chat"` and `SourceId = chatId`. If one exists, skip creating a new one (or optionally update the existing one's Title/Description/CreatedUtc).
- This could be a method on `INotificationService`: `Task<bool> HasUnreadSourceNotificationAsync(string recipientId, string sourceType, string sourceId)` or handled inline in the consumers.
- For `StartChatConsumer`: always create (it's a new chat, no existing notification possible).
- For `SendMessageConsumer`: check first, skip if unread notification already exists for that chat+recipient.

**Files:**

- Modify: `src/web/Jordnaer/Consumers/SendMessageConsumer.cs`
- Optionally modify: `src/web/Jordnaer/Features/Notifications/NotificationService.cs` (add helper method)

### 3. Retire UnreadMessageSignalRClient from TopBar and ChatNavLink

**Problem:** `TopBar.razor` still injects `UnreadMessageSignalRClient` to drive the Chat button's unread badge (`_unreadCount`). `ChatNavLink.razor` also injects it for the same purpose. This means every logged-in user opens an extra SignalR connection to `/hubs/chat` just for unread count tracking — redundant now that NotificationBell exists.

**What to do:**

- Remove the separate chat unread badge from the Chat nav button entirely. The NotificationBell already shows all unread notifications including chat. Users clicking the bell see chat notifications and can navigate to them. This simplifies the UI.
- Remove `UnreadMessageSignalRClient` injection and subscriptions from `TopBar.razor` and `ChatNavLink.razor`.
- After removal: delete `UnreadMessageSignalRClient.cs` and remove its DI registration.

**Files:**

- Modify: `src/web/Jordnaer/Pages/Shared/TopBar.razor`
- Modify: `src/web/Jordnaer/Pages/Shared/ChatNavLink.razor`
- Delete: `src/web/Jordnaer/Features/Chat/UnreadMessageSignalRClient.cs`
- Modify: `src/web/Jordnaer/Features/Chat/WebApplicationBuilderExtensions.cs` (remove registration)

### 4. Retire GroupMembershipHub / GroupMembershipSignalRClient / GroupMembershipNotificationService

**Problem:** `TopBar.razor` still uses `GroupMembershipSignalRClient` to show `_pendingGroupRequestCount` on the Groups nav button. `GroupService.UpdateMembership` and `MembershipService.RequestMembership` still call `IGroupMembershipNotificationService.NotifyAdminsOfPendingCountChangeAsync`. This is a third concurrent SignalR connection per user.

**What to do:**

- Remove the Groups pending badge from the nav button. Admins already get in-app notifications via the bell when someone requests membership. The bell is the single source of truth for all notification types.
- Remove `GroupMembershipSignalRClient` from TopBar.
- Remove `IGroupMembershipNotificationService` calls from `GroupService` and `MembershipService`.
- After removal: delete `GroupMembershipNotificationService.cs`, `GroupMembershipSignalRClient.cs`, `GroupMembershipHub.cs`, the hub interface, and remove DI registrations and the hub mapping from Program.cs.

**Files:**

- Modify: `src/web/Jordnaer/Pages/Shared/TopBar.razor`
- Modify: `src/web/Jordnaer/Features/Groups/GroupService.cs` (remove IGroupMembershipNotificationService)
- Modify: `src/web/Jordnaer/Features/Membership/MembershipService.cs` (remove IGroupMembershipNotificationService)
- Modify: `src/web/Jordnaer/Program.cs` (remove GroupMembershipHub mapping)
- Delete: `src/web/Jordnaer/Features/Groups/GroupMembershipNotificationService.cs`
- Delete: `src/web/Jordnaer/Features/Groups/GroupMembershipSignalRClient.cs`
- Delete: `src/web/Jordnaer/Features/Groups/GroupMembershipHub.cs`
- Modify: `src/web/Jordnaer/Features/Groups/WebApplicationBuilderExtensions.cs` (remove registrations)
- Modify: `tests/web/Jordnaer.Tests/Groups/GroupServiceTests.cs` (remove mock)

### 5. Stop Dual-Writing UnreadMessages (Optional — Can Defer)

**Problem:** Both consumers still create `UnreadMessage` rows alongside `Notification` rows. `ChatService` still reads `UnreadMessages` for per-chat unread counts and `MarkMessagesAsReadAsync`.

**What to do:**

- This is the biggest change and can be deferred if the per-chat unread count badge inside the chat list is still needed.
- `ChatService.GetChatsAsync` includes `.Count(unread => ...)` against `UnreadMessages` to populate `ChatDto.UnreadMessageCount`. This drives the per-chat-row badge in the chat list (e.g., "3" next to a chat name).
- The new `Notification` system only tracks one notification per chat (by SourceId), not per-message counts.
- **If per-chat unread counts are still needed:** keep `UnreadMessage` table and dual-writes for now. The `Notification` replaces the global unread badge and bell, while `UnreadMessage` continues to drive per-chat counts inside the chat page.
- **If per-chat counts can be dropped:** remove UnreadMessage writes from consumers, remove `UnreadMessage` reads from `ChatService`, remove `UnreadMessages` DbSet, and create a migration to drop the table.

**Decision needed:** Keep `UnreadMessage` for per-chat counts, or migrate that too?

**Files (if fully removing):**

- Modify: `src/web/Jordnaer/Consumers/SendMessageConsumer.cs`
- Modify: `src/web/Jordnaer/Consumers/StartChatConsumer.cs`
- Modify: `src/web/Jordnaer/Features/Chat/ChatService.cs`
- Modify: `src/web/Jordnaer/Pages/Chat/ChatPage.razor`
- Modify: `src/web/Jordnaer/Database/JordnaerDbContext.cs`
- Delete: `src/shared/Jordnaer.Shared/Database/UnreadMessage.cs`
- New EF migration to drop UnreadMessages table

### 6. Minor Issues

#### 6a. NotificationsPage has no real-time updates

The full `/notifications` page does not inject `NotificationSignalRClient`. New notifications arriving while the page is open won't appear until refresh.

**Fix:** Inject `NotificationSignalRClient`, subscribe to `OnNotificationReceived` to prepend new items, and `OnNotificationRead`/`OnUnreadCountChanged` for state sync.

**File:** `src/web/Jordnaer/Pages/Notifications/NotificationsPage.razor`

#### 6b. ChatPage doesn't clear notifications on inline message receipt

When a message arrives in an already-open chat, `ChatPage.razor` calls `MarkMessagesAsReadAsync` (old system) but doesn't call `NotificationService.MarkSourceAsReadAsync` (new system). The bell badge won't update.

**Fix:** In the `OnMessageReceived` handler, also call `NotificationService.MarkSourceAsReadAsync` when the received message's chat is the currently active chat.

**File:** `src/web/Jordnaer/Pages/Chat/ChatPage.razor`

#### 6d. GenericNotification email template missing baseUrl

`EmailContentBuilder.GenericNotification` doesn't accept a `baseUrl` parameter, so notification email links won't have the full URL. The `LinkUrl` stored on notifications is a relative path like `/chat/{id}`.

**Fix:** Either pass `baseUrl` to `GenericNotification`, or resolve the full URL in `NotificationService.PublishEmailAsync` before building the email.

**Files:**

- `src/web/Jordnaer/Features/Email/EmailContentBuilder.cs`
- `src/web/Jordnaer/Features/Notifications/NotificationService.cs`

### 7. Background Job — Purge Old Notifications

**Problem:** The Notifications table will grow indefinitely. Old read notifications serve no purpose and should be cleaned up automatically.

**What to do:**
- Create a `NotificationCleanupService` as a hosted `BackgroundService` that runs periodically (e.g., once per day).
- Delete notifications older than a configurable retention period (default: 6 months).
- The retention period should be configurable via `appsettings.json`:
  ```json
  {
    "Notifications": {
      "RetentionDays": 180
    }
  }
  ```
- Use `ExecuteDeleteAsync` for efficient bulk deletion:
  ```csharp
  var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
  await context.Notifications
      .Where(n => n.CreatedUtc < cutoff)
      .ExecuteDeleteAsync(ct);
  ```
- Log the number of deleted notifications.
- Register in `Features/Notifications/WebApplicationBuilderExtensions.cs`.

**Files:**
- Create: `src/web/Jordnaer/Features/Notifications/NotificationCleanupService.cs`
- Modify: `src/web/Jordnaer/Features/Notifications/WebApplicationBuilderExtensions.cs` (register hosted service)
- Modify: `appsettings.json` (add `Notifications:RetentionDays` setting)

## Suggested Implementation Order

1. **Retire ChatNotificationService** (item 1 + 2) — biggest win, removes dual email path
2. **Retire GroupMembership old hub** (item 4) — removes old SignalR hub and services
3. **Retire UnreadMessageSignalRClient from TopBar** (item 3) — removes old SignalR client
4. **Add notification cleanup job** (item 7)
5. **Minor fixes** (items 6a-6d)
6. **UnreadMessage table removal** (item 5) — defer or do last, most complex

## Acceptance Criteria

- [ ] `ChatNotificationService` deleted, no longer referenced
- [ ] Chat email delivery handled by NotificationService with ChatNotificationPreference checks
- [ ] SendMessageConsumer deduplicates notifications (one per chat, not per message)
- [ ] `UnreadMessageSignalRClient` removed from TopBar and ChatNavLink
- [ ] `GroupMembershipHub`, `GroupMembershipSignalRClient`, `GroupMembershipNotificationService` deleted
- [ ] `Program.cs` no longer maps `/hubs/group-membership`
- [ ] TopBar connects to only one notification SignalR hub
- [ ] NotificationsPage updates in real-time
- [ ] ChatPage clears notification bell on inline message receipt
- [ ] GenericNotification email template includes full URLs
- [ ] Background job purges notifications older than configured retention period
- [ ] Retention period configurable via `Notifications:RetentionDays` in appsettings
- [ ] All tests pass
- [ ] Solution builds with zero errors
