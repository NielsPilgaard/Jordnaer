# Task 04: Subscriptions (Users, Events, Groups)

## Context

**App:** Jordnaer (.NET Blazor Server)
**Area:** Discovery & Notifications
**Priority:** Medium
**Related:** None

## Objective

Allow users to subscribe to notifications when:

1. **New users** are created within a specified distance, optionally filtered by categories
2. **New events** are created within a specified distance, optionally filtered by categories
3. **New groups** are created within a specified distance, optionally filtered by categories

Additionally, show subscription prompts/banners in relevant location(s) throughout the app to encourage discovery.

---

## Part 1: New User Subscriptions

Allow users to subscribe to notifications when new users are created within a specified distance of their location, optionally filtered by categories (interests).

## Current State

- User profiles have location data via `Point Location` in [UserProfile.cs](src/shared/Jordnaer.Shared/Database/UserProfile.cs)
- Categories exist and link to users via [Category.cs](src/shared/Jordnaer.Shared/Database/Category.cs)
- Location service exists at [LocationService.cs](src/web/Jordnaer/Features/Profile/LocationService.cs)
- NetTopologySuite used for spatial queries (SRID 4326, WGS84)
- No subscription system exists

## Requirements

### 1. Data Model

Create subscription entity:

```csharp
public class NewUserSubscription
{
    public Guid Id { get; set; }
    public string UserProfileId { get; set; }

    // Location center point (subscriber's location or custom)
    public Point Location { get; set; }
    public string? LocationDescription { get; set; } // "København" or address for display

    // Radius in kilometers
    public int RadiusKm { get; set; }

    // Category filter (empty = all categories)
    public List<Category> Categories { get; set; } = [];

    // Notification settings
    public SubscriptionFrequency Frequency { get; set; } = SubscriptionFrequency.Immediate;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; }
    public DateTime? LastNotifiedUtc { get; set; }

    // Navigation
    public UserProfile UserProfile { get; set; }
}

public enum SubscriptionFrequency
{
    [Display(Name = "Med det samme")]
    Immediate = 0,
    [Display(Name = "Daglig opsummering")]
    Daily = 1,
    [Display(Name = "Ugentlig opsummering")]
    Weekly = 2
}
```

### 2. Subscription Management UI

Create page at `/settings/subscriptions`:

- **Create subscription form:**
  - Location input (use existing address autocomplete or "use my location")
  - Radius selector: 5km, 10km, 25km, 50km, 100km
  - Category multi-select (optional - empty means all)
  - Frequency selector

- **Active subscriptions list:**
  - Show location description, radius, categories, frequency
  - Toggle active/inactive
  - Delete subscription
  - Edit subscription

- **Limit:** Max 5 active subscriptions per user

### 3. Subscription Matching Service

Create `SubscriptionService`:

```csharp
// Called when new user completes profile
Task ProcessNewUserAsync(UserProfile newUser, CancellationToken ct = default);

// Find matching subscriptions for a new user
Task<List<NewUserSubscription>> FindMatchingSubscriptionsAsync(UserProfile newUser, CancellationToken ct = default);

// Send immediate notifications
Task SendImmediateNotificationsAsync(UserProfile newUser, List<NewUserSubscription> subscriptions, CancellationToken ct = default);

// CRUD operations
Task<NewUserSubscription> CreateSubscriptionAsync(NewUserSubscription subscription, CancellationToken ct = default);
Task<List<NewUserSubscription>> GetUserSubscriptionsAsync(string userId, CancellationToken ct = default);
Task UpdateSubscriptionAsync(NewUserSubscription subscription, CancellationToken ct = default);
Task DeleteSubscriptionAsync(Guid subscriptionId, CancellationToken ct = default);
```

### 4. Matching Logic

When a new user completes their profile (has location set):

0. Quickly offload the work out-of-process with MassTransit to ensure background processing
1. Query all active subscriptions
2. Filter by spatial distance: `subscription.Location.Distance(newUser.Location) <= subscription.RadiusKm * 1000`
3. Filter by categories: If subscription has categories, new user must have at least one matching
4. Exclude self-notifications (subscriber can't match themselves)
5. Group by frequency and process accordingly

### 5. Notification Delivery

**Immediate notifications:**

- Send email when match found
- Subject: "Ny bruger i dit område"
- Body: New user's display name, location (city only for privacy), shared categories, link to profile

### 6. Integration Points

Trigger subscription check when:

- User completes profile for first time (location set)
- User updates location to a new value
- Use hook in [ProfileService.cs](src/web/Jordnaer/Features/Profile/ProfileService.cs)

## Acceptance Criteria

### Data Model

- [ ] `NewUserSubscription` entity created
- [ ] Many-to-many relationship with Categories
- [ ] Database migration created

### Subscription UI

- [ ] Create subscription form with location, radius, categories
- [ ] List active subscriptions with edit/delete
- [ ] Toggle subscription active/inactive
- [ ] Max 5 subscriptions enforced
- [ ] Accessible from settings menu

### Matching & Notifications

- [ ] Spatial query correctly filters by distance
- [ ] Category filtering works (empty = match all)
- [ ] Immediate notifications sent on match
- [ ] Self-notifications prevented
- [ ] Email contains new user info and profile link

### Integration

- [ ] Triggered when user completes profile
- [ ] Triggered when user changes location
- [ ] Non-blocking (async processing)

## Files to Create/Modify

**New Files:**

- `src/shared/Jordnaer.Shared/Database/NewUserSubscription.cs`
- `src/shared/Jordnaer.Shared/Database/Enums/SubscriptionFrequency.cs`
- `src/web/Jordnaer/Features/Subscriptions/SubscriptionService.cs`
- `src/web/Jordnaer/Pages/Settings/Subscriptions.razor`
- Database migration

**Modify:**

- [JordnaerDbContext.cs](src/web/Jordnaer/Database/JordnaerDbContext.cs) - Add NewUserSubscriptions DbSet
- [ProfileService.cs](src/web/Jordnaer/Features/Profile/ProfileService.cs) - Trigger subscription check on profile complete/update
- [EmailService.cs](src/web/Jordnaer/Features/Email/EmailService.cs) - Add subscription notification method

## Technical Notes

- Use SQL Server's spatial functions for distance queries: `point.IsWithinDistance` returns meters
- NetTopologySuite Point uses SRID 4326 (WGS84) - distances in degrees, convert to meters
- Privacy: Only show city/area of new user, not exact address
- Index `NewUserSubscription.Location` for spatial query performance

---

## Part 2: New Event Subscriptions

### Objective

Allow users to subscribe to notifications when new events are created within a specified distance of their location, optionally filtered by categories.

### Data Model

```csharp
public class NewEventSubscription
{
    public Guid Id { get; set; }
    public string UserProfileId { get; set; }

    // Location center point (subscriber's location or custom)
    public Point Location { get; set; }
    public string? LocationDescription { get; set; }

    // Radius in kilometers
    public int RadiusKm { get; set; }

    // Category filter (empty = all categories)
    public List<Category> Categories { get; set; } = [];

    // Notification settings
    public SubscriptionFrequency Frequency { get; set; } = SubscriptionFrequency.Immediate;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; }
    public DateTime? LastNotifiedUtc { get; set; }

    // Navigation
    public UserProfile UserProfile { get; set; }
}
```

### Matching Logic

When a new event is created:

1. Quickly offload the work out-of-process with MassTransit to ensure background processing
2. Query all active event subscriptions
3. Filter by spatial distance: `subscription.Location.Distance(event.Location) <= subscription.RadiusKm * 1000`
4. Filter by categories: If subscription has categories, event must have at least one matching
5. Exclude self-notifications (event creator can't match themselves)
6. Group by frequency and process accordingly

### Notification Delivery

**Immediate notifications:**

- Send email when match found
- Subject: "Nyt arrangement i dit område"
- Body: Event name, date, location (city/area), categories, link to event

### Acceptance Criteria

- [ ] `NewEventSubscription` entity created
- [ ] Many-to-many relationship with Categories
- [ ] Database migration created
- [ ] Triggered when event is created
- [ ] Spatial query correctly filters by distance
- [ ] Category filtering works (empty = match all)
- [ ] Self-notifications prevented

---

## Part 3: New Group Subscriptions

### Objective

Allow users to subscribe to notifications when new groups are created within a specified distance of their location, optionally filtered by categories.

### Data Model

```csharp
public class NewGroupSubscription
{
    public Guid Id { get; set; }
    public string UserProfileId { get; set; }

    // Location center point (subscriber's location or custom)
    public Point Location { get; set; }
    public string? LocationDescription { get; set; }

    // Radius in kilometers
    public int RadiusKm { get; set; }

    // Category filter (empty = all categories)
    public List<Category> Categories { get; set; } = [];

    // Notification settings
    public SubscriptionFrequency Frequency { get; set; } = SubscriptionFrequency.Immediate;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; }
    public DateTime? LastNotifiedUtc { get; set; }

    // Navigation
    public UserProfile UserProfile { get; set; }
}
```

### Matching Logic

When a new group is created:

1. Quickly offload the work out-of-process with MassTransit to ensure background processing
2. Query all active group subscriptions
3. Filter by spatial distance: `subscription.Location.Distance(group.Location) <= subscription.RadiusKm * 1000`
4. Filter by categories: If subscription has categories, group must have at least one matching
5. Exclude self-notifications (group creator can't match themselves)
6. Group by frequency and process accordingly

### Notification Delivery

**Immediate notifications:**

- Send email when match found
- Subject: "Ny gruppe i dit område"
- Body: Group name, description snippet, location (city/area), categories, link to group

### Acceptance Criteria

- [ ] `NewGroupSubscription` entity created
- [ ] Many-to-many relationship with Categories
- [ ] Database migration created
- [ ] Triggered when group is created
- [ ] Spatial query correctly filters by distance
- [ ] Category filtering works (empty = match all)
- [ ] Self-notifications prevented

---

## Part 4: Subscription Prompts/Banners in App

### Objective

Show contextual subscription prompts in relevant locations to encourage users to subscribe and discover new content.

### Display Locations

#### 1. Groups Page / Group Search Results

Show banner at the top or bottom of group listings:

- **Trigger:** User views groups or performs a group search
- **Content:** "Få besked når nye grupper oprettes i dit område"
- **Action:** Button to create a new group subscription (pre-filled with current search location/categories if applicable)
- **Dismissible:** Yes, with "Don't show again" option (stored in user preferences)

#### 2. Posts/Feed Page

Show banner periodically in the feed:

- **Trigger:** User scrolls through posts/feed
- **Content:** "Bliv opdateret når nye brugere, grupper eller arrangementer dukker op i nærheden"
- **Action:** Link to subscription settings page
- **Frequency:** Show once per session, or once per week if dismissed

#### 3. User Search Results

Show banner in user search results:

- **Trigger:** User searches for other users
- **Content:** "Få besked når nye forældre tilmelder sig i dit område"
- **Action:** Button to create a new user subscription (pre-filled with current search location/categories)
- **Dismissible:** Yes

#### 4. Events Page / Event Search

Show banner on events listing:

- **Trigger:** User views events or searches for events
- **Content:** "Få besked når nye arrangementer oprettes i nærheden"
- **Action:** Button to create a new event subscription
- **Dismissible:** Yes

### Implementation

#### Subscription Banner Component

Create a reusable component:

```razor
@* SubscriptionBanner.razor *@
<div class="subscription-banner @(Dismissed ? "hidden" : "")">
    <p>@Message</p>
    <button @onclick="CreateSubscription">@ActionText</button>
    <button @onclick="Dismiss">Luk</button>
</div>

@code {
    [Parameter] public SubscriptionType Type { get; set; }
    [Parameter] public string Message { get; set; }
    [Parameter] public string ActionText { get; set; } = "Opret abonnement";
    [Parameter] public Point? PrefilledLocation { get; set; }
    [Parameter] public List<Category>? PrefilledCategories { get; set; }
}
```

#### User Preferences for Dismissal

Add to user preferences/settings:

```csharp
public class SubscriptionBannerPreferences
{
    public bool DismissedUserSubscriptionBanner { get; set; }
    public bool DismissedGroupSubscriptionBanner { get; set; }
    public bool DismissedEventSubscriptionBanner { get; set; }
    public DateTime? LastBannerShownUtc { get; set; }
}
```

### Acceptance Criteria

- [ ] Subscription banner component created
- [ ] Banner shown on Groups page
- [ ] Banner shown on User search results
- [ ] Banner shown on Events page
- [ ] Banner shown periodically in posts/feed
- [ ] Dismissal state persisted per user
- [ ] Pre-fill location/categories from current search context when applicable
- [ ] Banner links to subscription creation or settings page

---

## Part 5: Unified Subscription Management UI

### Objective

Consolidate all subscription types into a single settings page for easy management.

### Page: `/settings/subscriptions`

#### Tabs or Sections

1. **Nye brugere** - User subscriptions
2. **Nye arrangementer** - Event subscriptions
3. **Nye grupper** - Group subscriptions

#### Per Section

- **Create subscription form:**
  - Location input (use existing address autocomplete or "use my location")
  - Radius selector: 5km, 10km, 25km, 50km, 100km
  - Category multi-select (optional - empty means all)
  - Frequency selector

- **Active subscriptions list:**
  - Show location description, radius, categories, frequency
  - Toggle active/inactive
  - Delete subscription
  - Edit subscription

- **Limit:** Max 5 active subscriptions per type (15 total)

### Quick Subscribe Modal

When user clicks banner action button, show a modal that allows quick subscription creation without navigating away from current page.

### Acceptance Criteria

- [ ] Unified subscriptions page with tabs/sections for each type
- [ ] Create/edit/delete subscriptions for all three types
- [ ] Quick subscribe modal from banner actions
- [ ] Subscription limits enforced per type
- [ ] Active/inactive toggle for each subscription

---

## Files to Create/Modify (Complete List)

### New Files

**Database Entities:**

- `src/shared/Jordnaer.Shared/Database/NewUserSubscription.cs`
- `src/shared/Jordnaer.Shared/Database/NewEventSubscription.cs`
- `src/shared/Jordnaer.Shared/Database/NewGroupSubscription.cs`
- `src/shared/Jordnaer.Shared/Database/Enums/SubscriptionFrequency.cs`
- `src/shared/Jordnaer.Shared/Database/SubscriptionBannerPreferences.cs`

**Services:**

- `src/web/Jordnaer/Features/Subscriptions/SubscriptionService.cs`
- `src/web/Jordnaer/Features/Subscriptions/UserSubscriptionMatcher.cs`
- `src/web/Jordnaer/Features/Subscriptions/EventSubscriptionMatcher.cs`
- `src/web/Jordnaer/Features/Subscriptions/GroupSubscriptionMatcher.cs`

**UI Components:**

- `src/web/Jordnaer/Features/Subscriptions/SubscriptionBanner.razor`
- `src/web/Jordnaer/Features/Subscriptions/QuickSubscribeModal.razor`
- `src/web/Jordnaer/Pages/Settings/Subscriptions.razor`

**Database:**

- Database migration for all subscription tables

### Files to Modify

- [JordnaerDbContext.cs](src/web/Jordnaer/Database/JordnaerDbContext.cs) - Add DbSets for all subscription types
- [ProfileService.cs](src/web/Jordnaer/Features/Profile/ProfileService.cs) - Trigger user subscription check
- [EmailService.cs](src/web/Jordnaer/Features/Email/EmailService.cs) - Add subscription notification methods
- Event creation service - Trigger event subscription check
- Group creation service - Trigger group subscription check
- Groups page - Add subscription banner
- User search page - Add subscription banner
- Events page - Add subscription banner
- Posts/feed page - Add subscription banner

---

## Technical Notes

- Use SQL Server's spatial functions for distance queries: `point.IsWithinDistance` returns meters
- NetTopologySuite Point uses SRID 4326 (WGS84) - distances in degrees, convert to meters
- Privacy: Only show city/area in notifications, not exact addresses
- Index all subscription location fields for spatial query performance
- Use MassTransit for background processing of subscription matching
- Consider batching daily/weekly digest emails
- Subscription banners should be lightweight and not impact page load performance
