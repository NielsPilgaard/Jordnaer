# Task 01: Improve User Search Results Display

## Context

**App:** Jordnaer (.NET Blazor Server)
**Area:** User Search
**Priority:** High
**Related:** Task 07 (Map Search) - both are part of the "new search experience"

## ⚠️ Feature Flag Requirement
**CRITICAL:** This feature MUST be implemented behind the same feature flag as Task 07 (Map Search).

```csharp
// appsettings.json
{
  "FeatureFlags": {
    "EnableNewSearchExperience": false  // Controls BOTH map search AND improved display
  }
}
```

**Implementation Notes:**
- When `false`: Use existing search results display
- When `true`: Enable new card-based display with distance info (from map search)
- Single flag controls entire new search experience (map + improved UI)
- Design this UI with map integration in mind (distance display, etc.)

## Objective

Enhance the visual presentation and layout of user search results to make them more scannable and informative, with integration for map-based distance display from Task 07.

## Current State

User search results display needs improvement in terms of visual hierarchy and information presentation. Does not show distance from selected location or integrate with map view.

## Requirements

1. **Feature flag implementation** - Use same `EnableNewSearchExperience` flag as Task 07
2. Display user information in a clear, card-based layout
3. Ensure responsive design across mobile and desktop
4. Add user avatars and key profile details
5. Implement visual hierarchy for better scannability
6. **Integrate with map search** - Show distance from selected location when map search is used
7. Support both list view and map marker view (from Task 07)

## Acceptance Criteria

### Feature Flag
- [ ] Uses `EnableNewSearchExperience` feature flag (shared with Task 07)
- [ ] When flag is `false`, existing search display is used
- [ ] When flag is `true`, new card-based display with distance info is used

### Visual Design
- [ ] Search results use a modern card-based layout
- [ ] Each result card shows: avatar, name, location, and key profile info
- [ ] **Distance from selected point displayed** (e.g., "3.2 km away") when using map search
- [ ] Layout is fully responsive (mobile & desktop)
- [ ] Visual hierarchy makes results easy to scan
- [ ] Consistent with JordnaerPalette design system (Task 03)

### Integration with Map Search (Task 07)
- [ ] Cards can display distance information from map-based search
- [ ] Clicking card can highlight corresponding map marker (if map visible)
- [ ] Design works well in split view (map + list) and list-only view

## Files to Investigate

- User search components (likely in `src/web/Jordnaer/Features/`)
- Existing card components for reference (e.g., `GroupCard.razor`, `ChildProfileCard.razor`)
- Feature flag configuration service
- Distance display formatting utilities (from Task 08)
