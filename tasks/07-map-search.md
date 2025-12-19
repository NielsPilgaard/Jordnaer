# Task 07: Add Map to Search Pages

## Context
**App:** Jordnaer (.NET Blazor Server)  
**Area:** Search/Location  
**Priority:** Medium  
**Related:** Task 08 (NetTopologySuite) - provides distance calculation engine for radius-based search

## Objective
Integrate a map component into search pages to allow users to visually search for people/groups/posts near a specific location by selecting a point on the map and defining a search radius.

## Current State
Search pages lack visual location-based search capabilities. Users cannot easily search "near this location" using a map interface.

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

1. Integrate interactive map component into search pages
2. **Add address search box with DataForsyningen autocomplete** (like uploaded image)
3. Allow users to click map to select search center point
4. Provide radius selector (slider or dropdown)
5. When address selected, center map on that location
6. Extract lat/long from map selection or address lookup
7. Pass lat/long + radius to search backend (uses NetTopologySuite from Task 08)
8. Display results as map markers AND list view
9. Show distance from selected point for each result
10. Ensure responsive design and good performance

## Acceptance Criteria

- [ ] Map component integrated (recommend Leaflet.js - free, lightweight, Blazor-compatible)
- [ ] **Address search box with DataForsyningen autocomplete implemented**
- [ ] **Selecting address from autocomplete centers map on that location**
- [ ] **Radius circle updates when address is selected**
- [ ] Users can click/tap map to select search location (alternative to address search)
- [ ] Radius selector implemented as slider (like image) or dropdown
- [ ] Search circle/radius displayed on map
- [ ] Lat/long extracted from map selection or address lookup
- [ ] Backend search uses lat/long + radius (integrates with Task 08)
- [ ] Results displayed as markers on map with distance labels
- [ ] List view shows distance from selected point (e.g., "3.2 km away")
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

- Search page components (user search, group search)
- Location-based search logic/services
- User profile model (verify lat/long storage from Task 08)
- Existing coordinate handling code
- EditProfile address autocomplete (reuse DataForsyningen integration)
