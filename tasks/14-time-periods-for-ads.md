# Task: Add Time Period Controls for Partner Ads and Cards

## Objective
Add the ability to set a display time window for partners, controlling when their ads and partner cards are visible to users.

## Background
Currently, partners with `CanHaveAd=true` and `CanHavePartnerCard=true` are always displayed. This task adds time-based scheduling so partners can have their visibility restricted to specific date ranges.

## Requirements

### 1. Database Schema Changes
Add two new nullable columns to the `Partner` entity in [src/shared/Jordnaer.Shared/Database/Partner.cs](src/shared/Jordnaer.Shared/Database/Partner.cs):

```csharp
public DateTime? DisplayStartUtc { get; set; }  // When ads/card should start showing (null = no start restriction)
public DateTime? DisplayEndUtc { get; set; }    // When ads/card should stop showing (null = no end restriction)
```

Create a new EF Core migration for these columns.

### 2. Display Logic Updates
A partner should only be displayed if:
- `DisplayStartUtc` is null OR `DateTime.UtcNow >= DisplayStartUtc`
- `DisplayEndUtc` is null OR `DateTime.UtcNow <= DisplayEndUtc`

Update the following locations:

**[src/web/Jordnaer/Features/Ad/AdProvider.cs](src/web/Jordnaer/Features/Ad/AdProvider.cs)** - `GetAdsAsync()` method:
- Add time-based filtering to the database query that retrieves partners for ads

**[src/web/Jordnaer/Pages/Footer/Partners.razor](src/web/Jordnaer/Pages/Footer/Partners.razor)**:
- Filter database partners to only include those within their display window

### 3. Helper Method (Recommended)
Add a computed property or helper method to the `Partner` entity for reuse:

```csharp
public bool IsWithinDisplayWindow(DateTime utcNow) =>
    (DisplayStartUtc is null || utcNow >= DisplayStartUtc) &&
    (DisplayEndUtc is null || utcNow <= DisplayEndUtc);
```

### 4. Admin UI (Optional Enhancement)
If partner editing UI exists, add date pickers for setting `DisplayStartUtc` and `DisplayEndUtc`.

## Files to Modify
1. `src/shared/Jordnaer.Shared/Database/Partner.cs` - Add new properties
2. `src/web/Jordnaer/Features/Ad/AdProvider.cs` - Filter by display window
3. `src/web/Jordnaer/Pages/Footer/Partners.razor` - Filter partners list
4. Create new migration in `src/web/Jordnaer/Migrations/`

## Testing
1. Create a partner with `DisplayStartUtc` in the future → should NOT appear
2. Create a partner with `DisplayEndUtc` in the past → should NOT appear
3. Create a partner with current time within the window → should appear
4. Create a partner with both fields null → should always appear (backwards compatible)

## Notes
- Use `DateTime.UtcNow` consistently for comparisons
- Null values mean "no restriction" for that bound (backwards compatible with existing data)
- Consider whether the `HasPartnerCard` and `HasAdImage` computed properties should incorporate the time check, or keep time filtering separate in the query layer
