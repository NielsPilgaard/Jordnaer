# Task 07: Add Map to Search Pages (Users, then Groups, then Posts)

## Context

**App:** Jordnaer (.NET Blazor Server)
**Area:** Search/Location
**Priority:** Medium
**Related:** Task 08 (NetTopologySuite) - provides distance calculation engine for radius-based search

## ⚠️ Feature Flag Requirement

**CRITICAL:** This feature MUST be implemented behind a feature flag to allow easy rollback to existing working behavior.

```csharp
// appsettings.json
{
  "FeatureFlags": {
    "EnableNewSearchExperience": false  // Controls map search for posts, groups, and users
  }
}
```

**Implementation Notes:**

- When `false`: Use existing search UI/behavior (fallback)
- When `true`: Enable new map-based search experience
- This flag will also control Task 01 (User Search Display improvements) - both are part of the "new search experience"
- Allows A/B testing, gradual rollout, and instant rollback if issues arise

## Objective

Integrate a map component into search pages (**users, groups, AND posts**) to allow users to visually search for content near a specific location by selecting a point on the map and defining a search radius.

**Implementation Order:**
1. Start with [UserSearch.razor](src/web/Jordnaer/Pages/UserSearch/UserSearch.razor) - refine until we're happy
2. Then extend to Groups search
3. Then extend to Posts search

## Current State

Search pages lack visual location-based search capabilities. Users cannot easily search "near this location" using a map interface for any content type (posts, groups, or users).

## How It Works (Integration with Task 08)

1. **User Interaction (Two Methods):**

   **Method A: Click on Map**

   - User clicks/taps on map to select a location
   - User sets search radius (e.g., 5km, 10km, 25km, 50km)

   **Method B: Search by Address** ⭐ (See uploaded image example)

   - User types address in search box (e.g., "nordlyvej 20")
   - DataForsyningen autocomplete suggests addresses
   - User selects address from dropdown
   - Map centers on selected address with radius circle
   - User can adjust radius with slider

2. **Backend Processing:**

   - Map selection or address search produces lat/long coordinates
   - Coordinates + radius passed to search backend
   - NetTopologySuite calculates distances from selected point
   - Returns users/groups/posts within radius

3. **Visual Feedback:**
   - Map displays search radius as a circle
   - **NO markers/pins for individual results** (privacy)
   - List view updates with distance from selected point

## Requirements

### Core Feature (All Search Types)

1. **Feature flag implementation** - `EnableNewSearchExperience` controls all new search UI
2. Integrate interactive map component starting with **Users search**, then Groups, then Posts
3. **Reuse existing [AddressAutoComplete.razor](src/web/Jordnaer/Features/Profile/AddressAutoComplete.razor) component** with DataForsyningen
4. Allow users to click map to select search center point
5. Provide radius selector (slider or dropdown)
6. When address selected, center map on that location
7. Extract lat/long from map selection or address lookup
8. Pass lat/long + radius to search backend (uses NetTopologySuite from Task 08)
9. **Display ONLY search radius circle on map** - NO result markers (privacy)
10. Show distance from selected point for each result in list view
11. Ensure responsive design and good performance

### Generic Implementation Strategy

**Code Reusability:**

- Create generic/reusable map search component that works with any filter type
- Add interfaces or base class to [UserSearchFilter.cs](src/shared/Jordnaer.Shared/UserSearch/UserSearchFilter.cs)
- Extend to GroupSearchFilter and PostSearchFilter to implement same interface/base
- Avoid maintaining 3 separate search implementations

### Search Type Specific Requirements

**Users Search:** (implement first)

- Distance from selected point displayed in list
- **NO map markers** - only search radius circle shown

**Groups Search:** (implement second)

- Distance from selected point displayed in list
- **NO map markers** - only search radius circle shown

**Posts Search:** (implement third)

- Distance from selected point displayed in list
- **NO map markers** - only search radius circle shown

## Acceptance Criteria

### Feature Flag

- [ ] `EnableNewSearchExperience` feature flag implemented in appsettings.json
- [ ] When flag is `false`, existing search UI displays (no map)
- [ ] When flag is `true`, new map-based search displays
- [ ] Easy toggle between old/new behavior without code changes

### Map Integration (Phased Implementation)

**Phase 1: Users Search**
- [ ] Map component integrated for Users search (recommend Leaflet.js - free, lightweight, Blazor-compatible)
- [ ] **AddressAutoComplete.razor component integrated into user search form**
- [ ] **Selecting address from autocomplete centers map on that location**
- [ ] **Radius circle updates when address is selected**
- [ ] Users can click/tap map to select search location (alternative to address search)
- [ ] Radius selector implemented as slider (like image) or dropdown
- [ ] Search circle/radius displayed on map
- [ ] **NO result markers** - only radius circle shown
- [ ] Lat/long extracted from map selection or address lookup
- [ ] Backend search uses lat/long + radius (integrates with Task 08)
- [ ] Refine and validate with user search until happy with implementation

**Phase 2: Generic Implementation**
- [ ] Create interface/base class for search filters (ILocationSearchFilter or LocationSearchFilterBase)
- [ ] Refactor UserSearchFilter to implement/inherit from base
- [ ] Create generic map search component that works with any filter implementing interface

**Phase 3: Groups & Posts**
- [ ] Extend GroupSearchFilter to implement same interface/base
- [ ] Integrate generic map component into Groups search
- [ ] Extend PostSearchFilter to implement same interface/base
- [ ] Integrate generic map component into Posts search

### Results Display (All Search Types)

- [ ] **NO map markers for results** - only search radius circle displayed
- [ ] List view shows distance from selected point (e.g., "3.2 km away") for all types
- [ ] Responsive design (mobile & desktop)
- [ ] Good performance (lazy loading, efficient rendering)
- [ ] Accessible controls for map interaction
- [ ] User's current location can be used as starting point (with permission)

## Technical Considerations

- **Map Library:** Recommend Leaflet.js (free, no API costs, Blazor-compatible via JS interop)
- **Alternative:** Google Maps (requires API key and billing)
- **Address Search:** Use DataForsyningen autocomplete API (same as EditProfile)
- **Geolocation:** Request user permission to use current location as default
- **Mobile:** Optimize for touch interactions and data usage
- **Performance:** Lazy load map library, cluster markers if many results
- **Coordinate Format:** Ensure lat/long matches format expected by NetTopologySuite (WGS84)

## UI Reference

address search box ("nordlyvej 20"), map with radius circle, and distance slider (3 km)

## Implementation Suggestions

**Phase 1: User Search (Refine First)**
1. Add Leaflet.js via CDN or npm
2. Create Blazor map component wrapper with JS interop
3. Integrate existing [AddressAutoComplete.razor](src/web/Jordnaer/Features/Profile/AddressAutoComplete.razor) into [UserSearch.razor](src/web/Jordnaer/Pages/UserSearch/UserSearch.razor)
4. Wire up address selection to center map and update lat/long in filter
5. Add click-on-map handler to set location as alternative to address search
6. Add radius selector UI (slider recommended)
7. Display search radius circle on map (no result markers)
8. Update [UserSearchFilter.cs](src/shared/Jordnaer.Shared/UserSearch/UserSearchFilter.cs) to include lat/long
9. Backend already supports lat/long + radius via NetTopologySuite
10. Test and refine until satisfied with UX

**Phase 2: Make It Generic**
11. Extract common location search logic into interface/base class
12. Create generic map component that accepts any filter implementing interface
13. Refactor user search to use generic component

**Phase 3: Extend to Groups & Posts**
14. Apply same pattern to GroupSearchFilter and PostSearchFilter
15. Integrate generic map component into Groups and Posts search pages

## Files to Investigate

### Phase 1: User Search
- [UserSearch.razor](src/web/Jordnaer/Pages/UserSearch/UserSearch.razor) - main search page
- [UserSearchFilter.cs](src/shared/Jordnaer.Shared/UserSearch/UserSearchFilter.cs) - add lat/long properties
- [AddressAutoComplete.razor](src/web/Jordnaer/Features/Profile/AddressAutoComplete.razor) - reuse this component
- UserSearchService - verify it accepts lat/long + radius

### Phase 2: Generic Implementation
- Create `ILocationSearchFilter` interface or `LocationSearchFilterBase` class
- Create generic `LocationMapSearch.razor` component

### Phase 3: Groups & Posts
- GroupSearchFilter - extend with interface/base
- PostSearchFilter - extend with interface/base
- Groups search page
- Posts search page

### Feature Flag Implementation
- appsettings.json (add `EnableNewSearchExperience` flag)
- Configuration service/interface for feature flags
- Search page conditional rendering based on flag
