# Task 03: Notification Settings

## Context

**App:** Jordnaer (.NET Blazor Server)
**Area:** User Preferences & Email
**Priority:** Medium
**Related:** Task 02 (Group Notifications) - depends on this for respecting preferences

## Objective

Allow users to control their email notification preferences with granular control for group posts and simple toggle for chat messages.

## Current State

- No notification preferences exist in the data model
- All emails are sent unconditionally
- Chat notifications sent via [ChatNotificationService.cs](src/web/Jordnaer/Features/Chat/ChatNotificationService.cs)
- Group post notifications to be added in Task 02
- User profile exists at [UserProfile.cs](src/shared/Jordnaer.Shared/Database/UserProfile.cs)

## Requirements

### 1. Data Model

Add to [UserProfile.cs](src/shared/Jordnaer.Shared/Database/UserProfile.cs):
```csharp
// Chat notification preference
public ChatNotificationPreference ChatNotifications { get; set; } = ChatNotificationPreference.FirstMessageOnly;

// Group post notifications - granular per group (stored separately)
```

Create new enum:
```csharp
public enum ChatNotificationPreference
{
    [Display(Name = "Ingen")]
    None = 0,
    [Display(Name = "Kun første besked i ny samtale")]
    FirstMessageOnly = 1,
    [Display(Name = "Alle beskeder")]
    AllMessages = 2
}
```

Create new entity for per-group preferences:
```csharp
public class GroupNotificationSetting
{
    public string UserProfileId { get; set; }
    public Guid GroupId { get; set; }
    public bool EmailOnNewPost { get; set; } = true;

    // Navigation
    public UserProfile UserProfile { get; set; }
    public Group Group { get; set; }
}
```

### 2. Settings UI

Create settings page at `/settings/notifications`:

- **Chat notifications section:**
  - Radio buttons: None / First message only (default) / All messages
  - Description explaining each option

- **Group notifications section:**
  - List of user's groups with toggle for each
  - "Email me when there's a new post" toggle per group
  - Default: enabled for all groups
  - Bulk actions: "Enable all" / "Disable all"

### 3. Service Methods

Create `NotificationSettingsService`:
```csharp
Task<ChatNotificationPreference> GetChatPreferenceAsync(string userId);
Task SetChatPreferenceAsync(string userId, ChatNotificationPreference preference);
Task<bool> ShouldSendGroupPostEmailAsync(string userId, Guid groupId);
Task SetGroupPostPreferenceAsync(string userId, Guid groupId, bool enabled);
Task<List<GroupNotificationSetting>> GetGroupPreferencesAsync(string userId);
```

### 4. Integration Points

Modify [ChatNotificationService.cs](src/web/Jordnaer/Features/Chat/ChatNotificationService.cs):
- Check `ChatNotificationPreference` before sending
- For `AllMessages`: Send on every message
- For `FirstMessageOnly`: Only send on `StartChat` (current behavior)
- For `None`: Never send

Modify group post notification (Task 02):
- Check `GroupNotificationSetting.EmailOnNewPost` before including user in recipients

### 5. Default Behavior

When user joins a group:
- Auto-create `GroupNotificationSetting` with `EmailOnNewPost = true`
- Or use default `true` when no setting exists (implicit default)

## Acceptance Criteria

### Data Model
- [ ] `ChatNotificationPreference` enum created
- [ ] `UserProfile.ChatNotifications` field added with default `FirstMessageOnly`
- [ ] `GroupNotificationSetting` entity created
- [ ] Database migration created and applied

### Settings UI
- [ ] New page at `/settings/notifications`
- [ ] Chat preference radio buttons functional
- [ ] Per-group toggles displayed for user's groups
- [ ] Changes save immediately (no submit button needed)
- [ ] Accessible from user profile/settings menu

### Chat Notifications
- [ ] `None`: No chat emails sent
- [ ] `FirstMessageOnly`: Only on new chat start (current behavior)
- [ ] `AllMessages`: Email on every new message
- [ ] Preference checked before sending

### Group Notifications
- [ ] Per-group toggle respected when sending post emails
- [ ] New group memberships default to enabled
- [ ] Bulk enable/disable works correctly

## Files to Create/Modify

**New Files:**
- `src/shared/Jordnaer.Shared/Database/Enums/ChatNotificationPreference.cs`
- `src/shared/Jordnaer.Shared/Database/GroupNotificationSetting.cs`
- `src/web/Jordnaer/Features/Notifications/NotificationSettingsService.cs`
- `src/web/Jordnaer/Pages/Settings/Notifications.razor`
- Database migration

**Modify:**
- [UserProfile.cs](src/shared/Jordnaer.Shared/Database/UserProfile.cs) - Add ChatNotifications field
- [JordnaerDbContext.cs](src/web/Jordnaer/Database/JordnaerDbContext.cs) - Add GroupNotificationSettings DbSet
- [ChatNotificationService.cs](src/web/Jordnaer/Features/Chat/ChatNotificationService.cs) - Check preferences
- [MembershipService.cs](src/web/Jordnaer/Features/Membership/MembershipService.cs) - Create default setting on join

## Technical Notes

- Use implicit defaults (assume enabled if no `GroupNotificationSetting` exists) to avoid migration complexity
- Settings page should use auto-save pattern (debounced updates)
- Consider caching notification preferences to avoid DB lookups on every message
- Follow existing settings page patterns if any exist
