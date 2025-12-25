# Task 03: Advertisement Management System

## Context

**App:** Jordnaer (.NET Blazor Server)
**Area:** Advertisement Management & Display
**Priority:** High
**Related:**
- Task 01 (Sponsor Dashboard) - Sponsors will eventually manage their own ads
- Task 02 (Backoffice Claims) - Admin access required for ad management

## Objective

Create a database-backed advertisement management system to replace hardcoded ads in search results. This allows adding/updating ads without code deployments and provides analytics tracking. Keep it simple for the current single-sponsor scenario while being extensible for future multi-sponsor needs.

## Current State

- Ads are hardcoded in [UserSearchResultComponent.razor](src/web/Jordnaer/Features/UserSearch/UserSearchResultComponent.razor) (lines 84-92)
- Only one sponsor (Moon Creative) currently exists
- No analytics tracking for ad performance
- Changes require code deployment
- [SponsorAd.razor](src/web/Jordnaer/Features/Ad/SponsorAd.razor) exists but only used on old landing page
- New [AdCard.razor](src/web/Jordnaer/Features/Ad/AdCard.razor) component created for user search

## Requirements

### 1. Database Schema

Create `Advertisement` table with essential fields:

```sql
CREATE TABLE Advertisements (
    Id                  INT IDENTITY PRIMARY KEY,

    -- Content
    Title               NVARCHAR(200) NOT NULL,
    Description         NVARCHAR(500) NULL,
    ImagePath           NVARCHAR(500) NOT NULL,
    Link                NVARCHAR(500) NOT NULL,

    -- Display Control
    IsActive            BIT NOT NULL DEFAULT 1,
    Priority            INT NOT NULL DEFAULT 0,

    -- Scheduling
    StartDate           DATETIME2 NULL,
    EndDate             DATETIME2 NULL,

    -- Placement
    Placement           NVARCHAR(50) NOT NULL,  -- 'UserSearch', 'GroupSearch', etc.

    -- Analytics
    ViewCount           INT NOT NULL DEFAULT 0,
    ClickCount          INT NOT NULL DEFAULT 0,

    -- Audit
    CreatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    -- Indexes
    INDEX IX_Advertisements_Placement_Active (Placement, IsActive),
    INDEX IX_Advertisements_Dates (StartDate, EndDate),

    -- Constraints
    CONSTRAINT CK_Advertisement_Priority CHECK (Priority >= 0),
    CONSTRAINT CK_Advertisement_Dates CHECK (EndDate IS NULL OR StartDate IS NULL OR EndDate > StartDate)
);
```

### 2. Entity Model & DTOs

**Entity:**
```csharp
// src/web/Jordnaer/Features/Ads/Advertisement.cs
public class Advertisement
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string ImagePath { get; set; }
    public required string Link { get; set; }

    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 0;

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public required string Placement { get; set; }

    public int ViewCount { get; set; }
    public int ClickCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

**Constants:**
```csharp
// src/shared/Jordnaer.Shared/Ads/AdPlacement.cs
public static class AdPlacement
{
    public const string UserSearch = "UserSearch";
    public const string GroupSearch = "GroupSearch";
    public const string Dashboard = "Dashboard";
    public const string Feed = "Feed";
}
```

**DTO:**
```csharp
// src/shared/Jordnaer.Shared/Ads/AdvertisementDto.cs
public record AdvertisementDto
{
    public int Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string ImagePath { get; init; }
    public required string Link { get; init; }
}
```

### 3. Service Layer

Create simple service for fetching and tracking ads:

```csharp
// src/web/Jordnaer/Features/Ads/IAdvertisementService.cs
public interface IAdvertisementService
{
    Task<List<AdvertisementDto>> GetActiveAdsAsync(
        string placement,
        int count = 10,
        CancellationToken cancellationToken = default);

    Task RecordViewAsync(int advertisementId);
    Task RecordClickAsync(int advertisementId);
}

// src/web/Jordnaer/Features/Ads/AdvertisementService.cs
public class AdvertisementService : IAdvertisementService
{
    private readonly JordnaerDbContext _context;

    public async Task<List<AdvertisementDto>> GetActiveAdsAsync(
        string placement,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.Advertisements
            .Where(ad =>
                ad.Placement == placement &&
                ad.IsActive &&
                (ad.StartDate == null || ad.StartDate <= now) &&
                (ad.EndDate == null || ad.EndDate > now))
            .OrderByDescending(ad => ad.Priority)
            .ThenBy(_ => Guid.NewGuid())  // Random within same priority
            .Take(count)
            .Select(ad => new AdvertisementDto
            {
                Id = ad.Id,
                Title = ad.Title,
                Description = ad.Description,
                ImagePath = ad.ImagePath,
                Link = ad.Link
            })
            .ToListAsync(cancellationToken);
    }

    public async Task RecordViewAsync(int advertisementId)
    {
        await _context.Advertisements
            .Where(ad => ad.Id == advertisementId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(ad => ad.ViewCount, ad => ad.ViewCount + 1));
    }

    public async Task RecordClickAsync(int advertisementId)
    {
        await _context.Advertisements
            .Where(ad => ad.Id == advertisementId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(ad => ad.ClickCount, ad => ad.ClickCount + 1));
    }
}
```

### 4. Update UserSearchResultComponent

Replace hardcoded ad with service call:

```csharp
@inject IAdvertisementService AdService

private record SearchResultItem
{
    public bool IsAd { get; init; }
    public UserDto? User { get; init; }
    public AdvertisementDto? Ad { get; init; }  // Changed from individual fields
}

private List<AdvertisementDto> _ads = [];

protected override async Task OnParametersSetAsync()
{
    // Fetch ads when results change
    if (SearchResult.Users.Count > 0)
    {
        var adCount = (int)Math.Ceiling(SearchResult.Users.Count / 8.0);
        _ads = await AdService.GetActiveAdsAsync(AdPlacement.UserSearch, adCount);
    }
}

private List<SearchResultItem> GetItemsWithAds()
{
    var items = new List<SearchResultItem>();
    var adIndex = 0;

    for (int i = 0; i < SearchResult.Users.Count; i++)
    {
        items.Add(new SearchResultItem { User = SearchResult.Users[i] });

        // Insert ad after every 8th user
        if ((i + 1) % 8 == 0 && adIndex < _ads.Count)
        {
            items.Add(new SearchResultItem
            {
                IsAd = true,
                Ad = _ads[adIndex++]
            });
        }
    }

    return items;
}
```

Update rendering:
```razor
@if (item.IsAd && item.Ad is not null)
{
    <AdCard Link="@item.Ad.Link"
            ImagePath="@item.Ad.ImagePath"
            ImageAlt="@item.Ad.Title"
            Title="@item.Ad.Title"
            Description="@item.Ad.Description"
            AdvertisementId="@item.Ad.Id" />
}
```

### 5. Update AdCard Component

Add analytics tracking:

```csharp
[Parameter]
public int? AdvertisementId { get; set; }

@inject IAdvertisementService AdService

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender && AdvertisementId.HasValue)
    {
        await AdService.RecordViewAsync(AdvertisementId.Value);
    }
}

private async Task OnClick()
{
    if (AdvertisementId.HasValue)
    {
        await AdService.RecordClickAsync(AdvertisementId.Value);
    }
}
```

Update link in template:
```razor
<MudLink Href="@Link" Target="_blank" @onclick="OnClick" @onclick:stopPropagation="true">
```

### 6. Seed Data

Create migration with seed data for Moon Creative:

```csharp
migrationBuilder.InsertData(
    table: "Advertisements",
    columns: new[] { "Title", "Description", "ImagePath", "Link", "Placement", "IsActive", "Priority" },
    values: new object[]
    {
        "Moon Creative",
        "Professionel webudvikling og design",
        "images/ads/mooncreative_mobile.png",
        "https://www.mooncreative.dk/",
        "UserSearch",
        true,
        100
    });
```

### 7. Simple Admin Interface (Backoffice)

Create minimal CRUD at `/backoffice/advertisements`:

**Features:**
- List all ads with status (active/scheduled/expired)
- Create new ad (form with title, description, image, link, placement, dates, priority)
- Edit existing ads
- Toggle active/inactive
- View analytics (views, clicks, CTR)
- Delete ads

**Authorization:**
- Require admin claim (from Task 02)
- Use `[Authorize(Policy = "AdminOnly")]`

**UI Components:**
- MudTable for listing ads
- MudDialog for create/edit forms
- File upload using existing [ImageService.cs](src/web/Jordnaer/Features/Images/ImageService.cs)
- Container: `advertisements` (auto-created by ImageService)

## Acceptance Criteria

### Database & Models
- [ ] Advertisement table created with migration
- [ ] Advertisement entity created
- [ ] AdvertisementDto created
- [ ] AdPlacement constants defined
- [ ] DbSet added to JordnaerDbContext
- [ ] Seed data for Moon Creative ad

### Service Layer
- [ ] IAdvertisementService interface created
- [ ] AdvertisementService implemented
- [ ] GetActiveAdsAsync respects scheduling and priority
- [ ] RecordViewAsync increments ViewCount
- [ ] RecordClickAsync increments ClickCount
- [ ] Service registered in Program.cs DI

### Integration
- [ ] UserSearchResultComponent uses service instead of hardcoded ad
- [ ] AdCard tracks views on render
- [ ] AdCard tracks clicks on link click
- [ ] Ads display correctly in search results
- [ ] Multiple ads rotate if available
- [ ] No ads shown if none are active

### Backoffice Admin UI
- [ ] Page at `/backoffice/advertisements` with admin-only access
- [ ] List all advertisements
- [ ] Create new advertisement form
- [ ] Edit existing advertisement
- [ ] Toggle active/inactive
- [ ] Delete advertisement
- [ ] View analytics (views, clicks, CTR)
- [ ] Image upload using ImageService
- [ ] Validation for required fields

### Analytics
- [ ] View count increments when ad displayed
- [ ] Click count increments when ad clicked
- [ ] Analytics visible in backoffice
- [ ] CTR calculated correctly (clicks/views * 100)

## Files to Create

**New Files:**
- `src/web/Jordnaer/Features/Ads/Advertisement.cs` - Entity
- `src/shared/Jordnaer.Shared/Ads/AdvertisementDto.cs` - DTO
- `src/shared/Jordnaer.Shared/Ads/AdPlacement.cs` - Constants
- `src/web/Jordnaer/Features/Ads/IAdvertisementService.cs` - Interface
- `src/web/Jordnaer/Features/Ads/AdvertisementService.cs` - Service implementation
- `src/web/Jordnaer/Pages/Backoffice/Advertisements.razor` - Admin CRUD page
- `src/web/Jordnaer/Database/Migrations/YYYYMMDD_AddAdvertisements.cs` - Migration

**Modify:**
- [UserSearchResultComponent.razor](src/web/Jordnaer/Features/UserSearch/UserSearchResultComponent.razor) - Use service
- [AdCard.razor](src/web/Jordnaer/Features/Ad/AdCard.razor) - Add analytics tracking
- [JordnaerDbContext.cs](src/web/Jordnaer/Database/JordnaerDbContext.cs) - Add DbSet
- [Program.cs](src/web/Jordnaer/Program.cs) - Register service

## Technical Notes

- Keep it simple for single sponsor - no complex targeting yet
- Use EF Core ExecuteUpdate for efficient counter increments
- Analytics track aggregate numbers only (GDPR-friendly)
- Priority system allows control over ad frequency (higher = more often)
- Random rotation within same priority prevents ad fatigue
- Scheduling allows future campaigns to be prepared in advance
- Image paths stored relative to assets root (same as current system)
- File uploads use existing ImageService to Azure Blob Storage
- Admin UI reuses existing backoffice patterns

## Future Enhancements (NOT in this task)

These can be added later when needed:
- Geographic targeting (zip codes, cities)
- Category/interest targeting
- A/B testing variants
- Budget limits and billing
- Sponsor self-service (Task 01)
- Click-through tracking with unique URLs
- Impression frequency capping
- Detailed analytics dashboard with charts

## Migration Strategy

1. Create migration and seed Moon Creative ad
2. Deploy database changes
3. Update components to use service
4. Deploy code changes
5. Verify existing ad still shows correctly
6. Test analytics tracking
7. Remove hardcoded ad references once confirmed working

## Estimated Effort

**Total: 4-6 hours**

- Database & migrations: 1 hour
- Service implementation: 1 hour
- Component updates: 1 hour
- Backoffice CRUD UI: 2-3 hours
- Testing & verification: 1 hour

## Success Metrics

- ✅ No code deployment needed to add/update ads
- ✅ Analytics show real view/click counts
- ✅ Admins can manage ads via backoffice
- ✅ System scales to multiple ads without code changes
- ✅ Moon Creative ad continues showing in user search
