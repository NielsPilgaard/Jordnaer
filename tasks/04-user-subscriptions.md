# Task 04: User Subscriptions

## Context

**App:** Jordnaer (.NET Blazor Server)
**Area:** Discovery & Notifications
**Priority:** Medium
**Related:** None

## Objective

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
