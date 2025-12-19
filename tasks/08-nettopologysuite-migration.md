# Task 08: Implement NetTopologySuite for Distance Calculations

## Context
**App:** Jordnaer (.NET Blazor Server)  
**Area:** Backend/Location Services  
**Priority:** Medium  
**Related:** Task 07 (Map Search) - provides lat/long coordinates for radius-based search

## Objective
Implement NetTopologySuite for calculating distances between locations (users, groups, posts) to improve performance and reduce external API dependencies for distance operations.

## Current State
Distance calculations may be using external services or inefficient methods. Need local, performant distance calculation using stored lat/long coordinates.

## Scope Clarification

### ✅ Use NetTopologySuite For:
- Calculating distance between user locations
- Calculating distance between user and groups
- Calculating distance between user and posts
- Radius-based search (e.g., "find users within 50km of lat/long")
- Distance sorting and filtering

### ⚠️ Keep DataForsyningen For:
- **Address autocomplete** in EditProfile page
- Converting user-entered addresses to lat/long coordinates
- This is the ONLY use case for DataForsyningen going forward

## Privacy & UX Requirements

**CRITICAL:** Implement clear privacy messaging in EditProfile:

1. **Privacy Notice** - Add prominent text explaining:
   - "Your exact address is never displayed to other users"
   - "Address is only used to calculate distances for search results"
   - "Other users will only see approximate distance (e.g., '5 km away')"

2. **Zipcode Option** - Allow users to enter only zipcode instead of full address:
   - Provide checkbox/toggle: "Use only zipcode for privacy"
   - If selected, only use zipcode for lat/long lookup
   - Less precise but more private

3. **Data Storage** - Store in user profile:
   - `Latitude` (decimal)
   - `Longitude` (decimal)
   - `Address` (optional, for user reference only - never shown publicly)

## Requirements

1. Install and configure NetTopologySuite
2. Implement distance calculation service using stored lat/long coordinates
3. Replace any existing distance calculation logic
4. Ensure accuracy (use appropriate distance formula - Haversine or Vincenty)
5. Support radius-based queries for Task 07 (map search)
6. Add privacy messaging to EditProfile
7. Implement zipcode-only option

## Acceptance Criteria

- [ ] NetTopologySuite package installed
- [ ] Distance calculation service implemented using lat/long from database
- [ ] Distance calculations work for: user-to-user, user-to-group, user-to-post
- [ ] Radius-based search functional (e.g., "within X km of lat/long")
- [ ] Distance accuracy verified (compare with known distances)
- [ ] Performance is fast (no external API calls for distance)
- [ ] Privacy notice added to EditProfile page
- [ ] Zipcode-only option implemented in EditProfile
- [ ] DataForsyningen ONLY used for address autocomplete
- [ ] Related tests updated and passing

## Technical Notes

- **Distance Formula:** Use Haversine formula for most cases (accurate for < 1000km)
- **Coordinate System:** Ensure lat/long stored as WGS84 (standard GPS coordinates)
- **Performance:** Consider spatial indexing if querying large datasets
- **Integration with Task 07:** Map search will provide lat/long + radius for queries

## Files to Investigate

- EditProfile page/component (for privacy messaging and zipcode option)
- User profile model (verify lat/long storage)
- Search for existing `DataForsyningen` usage (distance vs autocomplete)
- Location/distance calculation services
- Search/filter logic for users, groups, posts
