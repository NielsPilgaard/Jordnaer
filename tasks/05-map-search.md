# Task 07: Add Map to Search Pages (Posts, Groups, Users)

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
Integrate a map component into ALL search pages (**posts, groups, AND users**) to allow users to visually search for content near a specific location by selecting a point on the map and defining a search radius.

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
   - Task 08's NetTopologySuite calculates distances from selected point
   - Returns users/groups/posts within radius

3. **Visual Feedback:**
   - Map displays search radius as a circle
   - Results shown as markers/pins on map
   - List view updates with distance from selected point

## Requirements

### Core Feature (All Search Types)
1. **Feature flag implementation** - `EnableNewSearchExperience` controls all new search UI
2. Integrate interactive map component into **all three search pages**: Posts, Groups, Users
3. **Add address search box with DataForsyningen autocomplete** (like uploaded image)
4. Allow users to click map to select search center point
5. Provide radius selector (slider or dropdown)
6. When address selected, center map on that location
7. Extract lat/long from map selection or address lookup
8. Pass lat/long + radius to search backend (uses NetTopologySuite from Task 08)
9. Display results as map markers AND list view
10. Show distance from selected point for each result
11. Ensure responsive design and good performance

### Search Type Specific Requirements

**Posts Search:**
- Map markers for posts (grouped by location if multiple posts at same place)
- Show post title/preview on marker click
- Distance from selected point displayed

**Groups Search:**
- Map markers for groups (based on group location)
- Show group name/member count on marker click
- Distance from selected point displayed

**Users Search:**
- Map markers for users (based on user profile location)
- Show user name/avatar on marker click
- Distance from selected point displayed
- Respect privacy settings (see Task 08 privacy requirements)

## Acceptance Criteria

### Feature Flag
- [ ] `EnableNewSearchExperience` feature flag implemented in appsettings.json
- [ ] When flag is `false`, existing search UI displays (no map)
- [ ] When flag is `true`, new map-based search displays
- [ ] Easy toggle between old/new behavior without code changes

### Map Integration (All Search Pages)
- [ ] Map component integrated for Posts search (recommend Leaflet.js - free, lightweight, Blazor-compatible)
- [ ] Map component integrated for Groups search
- [ ] Map component integrated for Users search
- [ ] **Address search box with DataForsyningen autocomplete implemented on all three**
- [ ] **Selecting address from autocomplete centers map on that location**
- [ ] **Radius circle updates when address is selected**
- [ ] Users can click/tap map to select search location (alternative to address search)
- [ ] Radius selector implemented as slider (like image) or dropdown
- [ ] Search circle/radius displayed on map
- [ ] Lat/long extracted from map selection or address lookup
- [ ] Backend search uses lat/long + radius (integrates with Task 08)

### Results Display (All Search Types)
- [ ] Posts displayed as markers on map with distance labels
- [ ] Groups displayed as markers on map with distance labels
- [ ] Users displayed as markers on map with distance labels
- [ ] List view shows distance from selected point (e.g., "3.2 km away") for all types
- [ ] Marker clustering for dense areas (performance optimization)
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

1. Add Leaflet.js via CDN or npm
2. Create Blazor component wrapper for map
3. Implement JS interop for map interactions
4. **Add address search input with DataForsyningen autocomplete**
5. **Wire up address selection to center map and update lat/long**
6. Add radius selector UI component (slider recommended, as shown in image)
7. Wire up search backend to accept lat/long + radius parameters
8. Render results as both map markers and list items
9. Test with various radius sizes and result counts
10. Test both interaction methods (click map vs search address)

## Files to Investigate

### Search Pages (All Three Types)
- Posts search page component
- Groups search page component
- Users search page component
- Location-based search logic/services

### Supporting Infrastructure
- User profile model (verify lat/long storage from Task 08)
- Group model (verify lat/long storage)
- Post model (verify lat/long storage - may need to add)
- Existing coordinate handling code
- EditProfile address autocomplete (reuse DataForsyningen integration)

### Feature Flag Implementation
- appsettings.json (add `EnableNewSearchExperience` flag)
- Configuration service/interface for feature flags
- Search page conditional rendering based on flag
