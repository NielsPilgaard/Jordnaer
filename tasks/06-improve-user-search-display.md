# Task 01: Improve User Search Results Display

## Context

**App:** Jordnaer (.NET Blazor Server)
**Area:** User Search
**Priority:** High
**Related:** Task 05 (Map Search) - both are part of the "new search experience"

## Objective

Enhance the visual presentation and layout of user search results to make them more scannable and informative. Since map search is now enabled by default, this task focuses on creating a modern card-based UI.

## Current State

User search results display needs improvement in terms of visual hierarchy and information presentation.

## Requirements

1. Display user information in a clear, card-based layout
2. Ensure responsive design across mobile and desktop
3. Add user avatars and key profile details
4. Implement visual hierarchy for better scannability
5. **Ad integration** - Design a non-invasive way to intertwine ads with search results (preparing for multiple ads in the future)

## Acceptance Criteria

### Visual Design

- [ ] Search results use a modern card-based layout
- [ ] Each result card shows: avatar, name, location, and key profile info
- [ ] Layout is fully responsive (mobile & desktop)
- [ ] Visual hierarchy makes results easy to scan
- [ ] Consistent with JordnaerPalette design system

### Ad Integration

- [ ] Ads are naturally intertwined with search results (not in a separate section)
- [ ] Ad placement is non-invasive and doesn't disrupt scanning
- [ ] Design scales to handle multiple ads in the future
- [ ] Ads are clearly distinguishable from regular results but blend visually

## Complexity Reduction & UX Improvements

**Simplify search experience:**

- Remove query string integration (no URL parameter syncing)
- Remove auto-search on navigation (only search on explicit user action)
- Use component-level state instead of URL state
- Simplify [UserSearchResultCache.cs](src/web/Jordnaer/Features/UserSearch/UserSearchResultCache.cs) usage

**Seamless navigation:**

- Improve scroll position restoration when navigating from search → profile/group → back to search
- Current implementation uses [scroll.js](src/web/Jordnaer/wwwroot/js/scroll.js) with 50ms setTimeout - this is janky
- Replace with proper scroll restoration:
  - Use Blazor's NavigationManager.LocationChanged event
  - Wait for OnAfterRenderAsync to ensure DOM is ready
  - Use IntersectionObserver API to restore to visible element instead of pixel position
  - Store both scroll position AND the visible item ID/index
  - More reliable restoration on different screen sizes
- Maintain search results in cache during navigation (avoid re-querying)
- Restore filter state when returning to search page

**Browser geolocation:**

- Use browser's Geolocation API to get user's current location as default search center
- Request location permission on search page load
- Set map center to user's coordinates if permission granted
- Fallback to Denmark center if permission denied or unavailable
- Show loading indicator while fetching location

## Files to Investigate

- User search components (likely in `src/web/Jordnaer/Features/`)
- Existing card components for reference (e.g., `GroupCard.razor`, `ChildProfileCard.razor`)
