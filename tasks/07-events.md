# Task: Implement Events (Begivenheder) Feature

## Overview
Implement a Facebook-like Events feature called "Begivenheder" (Danish for "events") that allows users to create, manage, and attend events within the Jordnaer platform.

## Tech Stack Context
- **Framework:** ASP.NET Core 10.0 Blazor Server
- **UI:** MudBlazor 8.15.0
- **ORM:** Entity Framework Core 10.0.2 with SQL Server
- **Spatial:** NetTopologySuite for location/maps
- **Messaging:** MassTransit 8.5.7 (for async email notifications)
- **Real-time:** SignalR (for live updates)
- **Validation:** FluentValidation 12.1.1

## Feature Requirements

### Core Event Properties
- **Title** (required)
- **Description** (rich text, using existing TextEditorComponent)
- **Date & Time** (start datetime, end datetime)
- **Location** (Address, ZipCode, City, Point coordinates - same pattern as Groups)
- **Fee/Cost** (optional decimal, with currency display)
- **Categories** (multi-select, reuse existing Category system)
- **Cover Image** (optional URL)

### Ownership & Permissions
- **Owner Types:**
  - User-owned event (CreatedByUserId)
  - Group-owned event (optional GroupId - only group admins can create)
- **Visibility Options** (enum `EventVisibility`):
  - `Public` - Anyone can view and RSVP
  - `Private` - Invite-only, not listed publicly
  - `MembersCanInvite` - Attendees can invite others

### RSVP/Attendance System
- **Attendance Statuses** (enum `EventAttendanceStatus`):
  - `Attending`
  - `Maybe`
  - `NotAttending`
  - `Interested`
- Track RSVPs via `EventAttendance` junction table

### Invitations
- Email-based invitation system (follow `PendingGroupInvite` pattern)
- Token-based invite links with expiration
- Track who invited whom

### Recurrence
- **Recurrence Pattern** (enum `RecurrencePattern`):
  - `None` (one-time event)
  - `Daily`
  - `Weekly`
  - `Biweekly`
  - `Monthly`
- `RecurrenceEndDate` (nullable DateTime)
- When recurring, generate child events or use a parent-child relationship

### Notifications
- **Email notifications** (via MassTransit consumers):
  - Event created (to group members if group-owned)
  - Event updated (to attendees)
  - Event reminder (configurable: 1 day, 1 hour before)
  - New RSVP (to event owner)
- **Real-time notifications** (SignalR):
  - Attendee count changes
  - Event updates

---

## Implementation Steps

### Phase 1: Database Models & Migrations

#### 1.1 Create Enums
Create in `src/shared/Jordnaer.Shared/Database/Enums/`:

**EventVisibility.cs**
```csharp
namespace Jordnaer.Shared.Database.Enums;

public enum EventVisibility
{
    Public = 0,
    Private = 1,
    MembersCanInvite = 2
}
```

**EventAttendanceStatus.cs**
```csharp
namespace Jordnaer.Shared.Database.Enums;

public enum EventAttendanceStatus
{
    Interested = 0,
    Attending = 1,
    Maybe = 2,
    NotAttending = 3
}
```

**RecurrencePattern.cs**
```csharp
namespace Jordnaer.Shared.Database.Enums;

public enum RecurrencePattern
{
    None = 0,
    Daily = 1,
    Weekly = 2,
    Biweekly = 3,
    Monthly = 4
}
```

#### 1.2 Create Entity Models
Create in `src/shared/Jordnaer.Shared/Database/`:

**Event.cs** (follow Group.cs pattern)
```csharp
namespace Jordnaer.Shared.Database;

using NetTopologySuite.Geometries;
using Jordnaer.Shared.Database.Enums;

public class Event
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }

    // Date & Time
    public DateTime StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }

    // Location (same pattern as Group)
    public string? Address { get; set; }
    public int? ZipCode { get; set; }
    public string? City { get; set; }
    public Point? Location { get; set; }

    // Cost
    public decimal? Fee { get; set; }
    public string? FeeCurrency { get; set; } // Default "DKK"

    // Media
    public string? CoverImageUrl { get; set; }

    // Visibility & Settings
    public EventVisibility Visibility { get; set; } = EventVisibility.Public;

    // Recurrence
    public RecurrencePattern RecurrencePattern { get; set; } = RecurrencePattern.None;
    public DateTime? RecurrenceEndDateUtc { get; set; }
    public Guid? ParentEventId { get; set; } // For recurring event instances
    public Event? ParentEvent { get; set; }
    public ICollection<Event> RecurringInstances { get; set; } = [];

    // Ownership
    public required string CreatedByUserId { get; set; }
    public UserProfile? CreatedBy { get; set; }
    public Guid? GroupId { get; set; } // Optional group ownership
    public Group? Group { get; set; }

    // Metadata
    public DateTime CreatedUtc { get; set; }
    public DateTime? LastUpdatedUtc { get; set; }

    // Relationships
    public ICollection<EventAttendance> Attendances { get; set; } = [];
    public ICollection<EventCategory> Categories { get; set; } = [];
    public ICollection<PendingEventInvite> PendingInvites { get; set; } = [];
}
```

**EventAttendance.cs** (follow GroupMembership.cs pattern)
```csharp
namespace Jordnaer.Shared.Database;

using Jordnaer.Shared.Database.Enums;

public class EventAttendance
{
    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public required string UserProfileId { get; set; }
    public UserProfile? UserProfile { get; set; }

    public EventAttendanceStatus Status { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? LastUpdatedUtc { get; set; }

    // Notification preferences
    public bool NotifyOnUpdates { get; set; } = true;
    public bool NotifyReminder { get; set; } = true;
}
```

**EventCategory.cs** (follow GroupCategory.cs pattern)
```csharp
namespace Jordnaer.Shared.Database;

public class EventCategory
{
    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }
}
```

**PendingEventInvite.cs** (follow PendingGroupInvite.cs pattern exactly)
```csharp
namespace Jordnaer.Shared.Database;

using Jordnaer.Shared.Database.Enums;

public class PendingEventInvite
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public required string Email { get; set; }
    public required string TokenHash { get; set; }
    public PendingInviteStatus Status { get; set; } = PendingInviteStatus.Pending;

    public DateTime CreatedUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }

    public string? InvitedByUserId { get; set; }
    public UserProfile? InvitedBy { get; set; }
}
```

#### 1.3 Update DbContext
Add to `src/web/Jordnaer/Database/JordnaerDbContext.cs`:

```csharp
public DbSet<Event> Events => Set<Event>();
public DbSet<EventAttendance> EventAttendances => Set<EventAttendance>();
public DbSet<EventCategory> EventCategories => Set<EventCategory>();
public DbSet<PendingEventInvite> PendingEventInvites => Set<PendingEventInvite>();
```

Configure in `OnModelCreating`:
```csharp
// Event
modelBuilder.Entity<Event>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
    entity.Property(e => e.Fee).HasPrecision(10, 2);
    entity.Property(e => e.FeeCurrency).HasMaxLength(3);

    entity.HasOne(e => e.CreatedBy)
        .WithMany()
        .HasForeignKey(e => e.CreatedByUserId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasOne(e => e.Group)
        .WithMany()
        .HasForeignKey(e => e.GroupId)
        .OnDelete(DeleteBehavior.SetNull);

    entity.HasOne(e => e.ParentEvent)
        .WithMany(e => e.RecurringInstances)
        .HasForeignKey(e => e.ParentEventId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasIndex(e => e.StartsAtUtc);
    entity.HasIndex(e => e.GroupId);
    entity.HasIndex(e => e.CreatedByUserId);
    entity.HasIndex(e => e.ZipCode);
});

// EventAttendance
modelBuilder.Entity<EventAttendance>(entity =>
{
    entity.HasKey(ea => new { ea.EventId, ea.UserProfileId });

    entity.HasOne(ea => ea.Event)
        .WithMany(e => e.Attendances)
        .HasForeignKey(ea => ea.EventId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(ea => ea.UserProfile)
        .WithMany()
        .HasForeignKey(ea => ea.UserProfileId)
        .OnDelete(DeleteBehavior.Cascade);
});

// EventCategory
modelBuilder.Entity<EventCategory>(entity =>
{
    entity.HasKey(ec => new { ec.EventId, ec.CategoryId });

    entity.HasOne(ec => ec.Event)
        .WithMany(e => e.Categories)
        .HasForeignKey(ec => ec.EventId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(ec => ec.Category)
        .WithMany()
        .HasForeignKey(ec => ec.CategoryId)
        .OnDelete(DeleteBehavior.Cascade);
});

// PendingEventInvite
modelBuilder.Entity<PendingEventInvite>(entity =>
{
    entity.HasKey(pei => pei.Id);
    entity.Property(pei => pei.Email).HasMaxLength(256).IsRequired();
    entity.Property(pei => pei.TokenHash).HasMaxLength(128).IsRequired();

    entity.HasOne(pei => pei.Event)
        .WithMany(e => e.PendingInvites)
        .HasForeignKey(pei => pei.EventId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(pei => pei.InvitedBy)
        .WithMany()
        .HasForeignKey(pei => pei.InvitedByUserId)
        .OnDelete(DeleteBehavior.SetNull);

    entity.HasIndex(pei => new { pei.Email, pei.EventId }).IsUnique();
    entity.HasIndex(pei => pei.TokenHash).IsUnique();
});
```

#### 1.4 Create Migration
Run:
```bash
dotnet ef migrations add AddEvents -p src/web/Jordnaer -s src/web/Jordnaer
```

---

### Phase 2: DTOs and Shared Types

Create in `src/shared/Jordnaer.Shared/Events/`:

**EventSlim.cs** (for list views)
```csharp
namespace Jordnaer.Shared.Events;

using Jordnaer.Shared.Database.Enums;

public record EventSlim(
    Guid Id,
    string Title,
    DateTime StartsAtUtc,
    DateTime? EndsAtUtc,
    string? City,
    int? ZipCode,
    decimal? Fee,
    string? CoverImageUrl,
    EventVisibility Visibility,
    int AttendingCount,
    int InterestedCount,
    string CreatedByUserId,
    string? CreatedByDisplayName,
    Guid? GroupId,
    string? GroupName
);
```

**EventDto.cs** (full details)
```csharp
namespace Jordnaer.Shared.Events;

using Jordnaer.Shared.Database.Enums;

public record EventDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartsAtUtc,
    DateTime? EndsAtUtc,
    string? Address,
    int? ZipCode,
    string? City,
    double? Latitude,
    double? Longitude,
    decimal? Fee,
    string? FeeCurrency,
    string? CoverImageUrl,
    EventVisibility Visibility,
    RecurrencePattern RecurrencePattern,
    DateTime? RecurrenceEndDateUtc,
    string CreatedByUserId,
    string? CreatedByDisplayName,
    string? CreatedByProfilePictureUrl,
    Guid? GroupId,
    string? GroupName,
    DateTime CreatedUtc,
    List<int> CategoryIds,
    List<EventAttendeeDto> Attendees
);

public record EventAttendeeDto(
    string UserProfileId,
    string DisplayName,
    string? ProfilePictureUrl,
    EventAttendanceStatus Status,
    DateTime CreatedUtc
);
```

**EventCreated.cs** (MassTransit event)
```csharp
namespace Jordnaer.Shared.Events;

public record EventCreated(
    Guid EventId,
    string Title,
    DateTime StartsAtUtc,
    string CreatedByUserId,
    Guid? GroupId
);
```

**EventUpdated.cs**
```csharp
namespace Jordnaer.Shared.Events;

public record EventUpdated(
    Guid EventId,
    string Title,
    DateTime StartsAtUtc
);
```

---

### Phase 3: Service Layer

Create directory: `src/web/Jordnaer/Features/Events/`

**EventService.cs** (follow GroupService.cs pattern)
```csharp
// Key methods to implement:
// - CreateEventAsync(Event event, List<int> categoryIds)
// - UpdateEventAsync(Guid eventId, Event updates, List<int> categoryIds)
// - DeleteEventAsync(Guid eventId)
// - GetEventByIdAsync(Guid eventId)
// - GetEventsAsync(EventFilter filter) // with pagination
// - GetUpcomingEventsForUserAsync(string userId)
// - GetEventsForGroupAsync(Guid groupId)
// - UpdateAttendanceAsync(Guid eventId, string userId, EventAttendanceStatus status)
// - GetAttendeesAsync(Guid eventId, EventAttendanceStatus? statusFilter)
// - CanUserEditEvent(Guid eventId, string userId) // Owner or group admin
```

Use `OneOf<Success, Error<string>>` and `OneOf<T, NotFound>` return patterns.

**EventInvitationService.cs** (follow PendingGroupInvite handling)
```csharp
// Key methods:
// - CreateInviteAsync(Guid eventId, string email, string invitedByUserId)
// - AcceptInviteAsync(string token)
// - GetPendingInvitesForEventAsync(Guid eventId)
// - RevokeInviteAsync(Guid inviteId)
```

---

### Phase 4: MassTransit Consumers

Create in `src/web/Jordnaer/Consumers/`:

**EventCreatedConsumer.cs** (follow GroupPostCreatedConsumer.cs pattern)
- Send email notifications to group members (if group-owned event)
- Include event details and link

**EventReminderConsumer.cs**
- Triggered by scheduled job (or use Hangfire/Quartz if available)
- Send reminder emails to attendees 24h and 1h before event

---

### Phase 5: UI Pages

Create in `src/web/Jordnaer/Pages/Events/`:

**ListEvents.razor** (main events page at `/begivenheder`)
- Filterable list: upcoming, past, my events, by category, by location
- Use MudDataGrid or MudStack with EventCard components
- Pagination support

**EventDetails.razor** (`/begivenheder/{id}`)
- Full event information display
- RSVP buttons (Attending/Maybe/Not Attending)
- Attendee list with avatars
- Edit/Delete buttons for owner/admins
- Share and invite functionality
- Map display if location is set

**CreateEvent.razor** (`/begivenheder/opret`)
- Form with all event fields
- Category selector (reuse CategorySelector.razor)
- Location picker
- Recurrence options
- Visibility selector

**EditEvent.razor** (`/begivenheder/{id}/rediger`)
- Same form as create, pre-populated
- Only accessible to owner/group admins

**MyEvents.razor** (`/mine-begivenheder`)
- Events user is attending/interested in
- Events user created
- Past events section

---

### Phase 6: UI Components

Create in `src/web/Jordnaer/Features/Events/`:

**EventCard.razor**
- Compact card for list views
- Shows: title, date/time, location, attendee count, cover image thumbnail
- Click to navigate to details

**EventForm.razor**
- Reusable form component for create/edit
- All event fields with validation
- Uses TextEditorComponent for description
- CategorySelector for categories

**EventAttendeesList.razor**
- Display attendees grouped by status
- Avatar grid with overflow handling

**RSVPButton.razor**
- Dropdown or button group for RSVP status
- Shows current user's status
- Handles status updates

---

### Phase 7: Navigation & Layout

1. Add "Begivenheder" to main navigation in `src/web/Jordnaer/Components/Layout/`:
   - Find NavMenu or similar component
   - Add link: `/begivenheder`

2. Add route registrations if using a central routing config

---

### Phase 8: Validation

Create `EventValidator.cs` using FluentValidation:
```csharp
public class EventValidator : AbstractValidator<Event>
{
    public EventValidator()
    {
        RuleFor(e => e.Title)
            .NotEmpty().WithMessage("Titel er påkrævet")
            .MaximumLength(200).WithMessage("Titel må højst være 200 tegn");

        RuleFor(e => e.StartsAtUtc)
            .GreaterThan(DateTime.UtcNow).WithMessage("Starttidspunkt skal være i fremtiden")
            .When(e => e.Id == Guid.Empty); // Only for new events

        RuleFor(e => e.EndsAtUtc)
            .GreaterThan(e => e.StartsAtUtc)
            .When(e => e.EndsAtUtc.HasValue)
            .WithMessage("Sluttidspunkt skal være efter starttidspunkt");

        RuleFor(e => e.Fee)
            .GreaterThanOrEqualTo(0)
            .When(e => e.Fee.HasValue)
            .WithMessage("Pris kan ikke være negativ");

        RuleFor(e => e.RecurrenceEndDateUtc)
            .GreaterThan(e => e.StartsAtUtc)
            .When(e => e.RecurrencePattern != RecurrencePattern.None && e.RecurrenceEndDateUtc.HasValue)
            .WithMessage("Gentagelsens slutdato skal være efter starttidspunktet");
    }
}
```

---

## File Reference Summary

### Files to Create
| Path | Purpose |
|------|---------|
| `src/shared/Jordnaer.Shared/Database/Enums/EventVisibility.cs` | Visibility enum |
| `src/shared/Jordnaer.Shared/Database/Enums/EventAttendanceStatus.cs` | RSVP status enum |
| `src/shared/Jordnaer.Shared/Database/Enums/RecurrencePattern.cs` | Recurrence enum |
| `src/shared/Jordnaer.Shared/Database/Event.cs` | Main event entity |
| `src/shared/Jordnaer.Shared/Database/EventAttendance.cs` | RSVP junction table |
| `src/shared/Jordnaer.Shared/Database/EventCategory.cs` | Category junction table |
| `src/shared/Jordnaer.Shared/Database/PendingEventInvite.cs` | Invitation entity |
| `src/shared/Jordnaer.Shared/Events/EventSlim.cs` | List DTO |
| `src/shared/Jordnaer.Shared/Events/EventDto.cs` | Full DTO |
| `src/shared/Jordnaer.Shared/Events/EventCreated.cs` | MassTransit event |
| `src/shared/Jordnaer.Shared/Events/EventUpdated.cs` | MassTransit event |
| `src/web/Jordnaer/Features/Events/EventService.cs` | Main service |
| `src/web/Jordnaer/Features/Events/EventInvitationService.cs` | Invitation handling |
| `src/web/Jordnaer/Features/Events/EventValidator.cs` | FluentValidation |
| `src/web/Jordnaer/Features/Events/EventCard.razor` | List card component |
| `src/web/Jordnaer/Features/Events/EventForm.razor` | Create/Edit form |
| `src/web/Jordnaer/Features/Events/EventAttendeesList.razor` | Attendees display |
| `src/web/Jordnaer/Features/Events/RSVPButton.razor` | RSVP component |
| `src/web/Jordnaer/Pages/Events/ListEvents.razor` | Main list page |
| `src/web/Jordnaer/Pages/Events/EventDetails.razor` | Detail page |
| `src/web/Jordnaer/Pages/Events/CreateEvent.razor` | Create page |
| `src/web/Jordnaer/Pages/Events/EditEvent.razor` | Edit page |
| `src/web/Jordnaer/Pages/Events/MyEvents.razor` | User's events |
| `src/web/Jordnaer/Consumers/EventCreatedConsumer.cs` | Email notifications |

### Files to Modify
| Path | Change |
|------|--------|
| `src/web/Jordnaer/Database/JordnaerDbContext.cs` | Add DbSets and configurations |
| `src/web/Jordnaer/Components/Layout/NavMenu.razor` (or similar) | Add navigation link |

### Key Reference Files (Read for Patterns)
| Path | Pattern to Follow |
|------|-------------------|
| `src/shared/Jordnaer.Shared/Database/Group.cs` | Entity structure with location |
| `src/shared/Jordnaer.Shared/Database/GroupMembership.cs` | Junction table pattern |
| `src/shared/Jordnaer.Shared/Database/PendingGroupInvite.cs` | Invitation pattern |
| `src/web/Jordnaer/Features/Groups/GroupService.cs` | Service layer pattern (550+ lines) |
| `src/web/Jordnaer/Features/GroupPosts/GroupPostService.cs` | Event publishing pattern |
| `src/web/Jordnaer/Consumers/GroupPostCreatedConsumer.cs` | Consumer pattern |
| `src/web/Jordnaer/Features/Category/CategorySelector.razor` | Category UI component |
| `src/web/Jordnaer/Pages/Groups/GroupDetails.razor` | Page structure with permissions |
| `src/web/Jordnaer/Features/GroupPosts/GroupPostForm.razor` | Form component pattern |

---

## Categories to Add

Add these event-specific categories to the Categories table if they don't exist:
- "Friluftsliv" (Outdoor life/nature activities)
- "Sport" (Sports activities)
- "Sociale arrangementer" (Social gatherings)
- "Workshops" (Workshops/classes)
- "Familievenligt" (Family-friendly)

---

## Testing Considerations

1. Create events as both user-owned and group-owned
2. Test all visibility modes (Public, Private, MembersCanInvite)
3. Test RSVP flow for each status
4. Test invitation flow with email
5. Test recurring events generation
6. Test permission checks (only owner/admin can edit)
7. Test category filtering
8. Test location-based queries
