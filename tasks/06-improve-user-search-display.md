# Task 06: Improve User Search Results Display

## Context

**App:** Jordnaer (.NET Blazor Server)
**Area:** User Search & Group Search Results Display
**Priority:** High
**Related:** Task 05 (Map Search) - both are part of the "new search experience"

## Objective

Enhance the visual presentation and layout of search results (both user and group) to make them more scannable and informative. Since map search is now enabled by default, this task focuses on creating a modern card-based UI with integrated advertisements.

---

## ✅ COMPLETED - User Search Improvements

**Note:** User search layout was improved first as a foundation. The modern card-based design and ad integration patterns established here should be replicated for group search.

### What Was Done

#### 1. Modern Card-Based Layout

- ✅ Created [UserCard.razor](src/web/Jordnaer/Features/UserSearch/UserCard.razor) component
- ✅ Clean, scannable layout with avatar, name, username, and location
- ✅ Visual hierarchy with proper spacing and typography
- ✅ Responsive grid: 1 column (mobile), 2 (tablet), 3-4 (desktop)
- ✅ Hover effects for better interactivity
- ✅ Smart display of children (with ages) and interests (first 4 + count)

#### 2. Ad Integration

- ✅ Created [AdCard.razor](src/web/Jordnaer/Features/Ad/AdCard.razor) component
- ✅ Created [HardcodedAds.cs](src/web/Jordnaer/Features/Ad/HardcodedAds.cs) - centralized ad data
- ✅ Ads intertwined naturally with results (not separate section)
- ✅ Displays every 8 users, or at end if < 8 results
- ✅ Always shows at least 1 ad if there are any results
- ✅ Clearly marked as "Annonce" but blends visually
- ✅ Scales to multiple ads (ready for Task 03 database system)

#### 3. Simplified Search Experience

- ✅ Updated [UserSearch.razor](src/web/Jordnaer/Pages/UserSearch/UserSearch.razor)
- ✅ Removed query string integration (no URL parameters)
- ✅ Removed auto-search on navigation
- ✅ Component-level state only
- ✅ Simplified cache usage - restores filter + results on back navigation
- ✅ Updated [UserSearchForm.razor](src/web/Jordnaer/Features/UserSearch/UserSearchForm.razor)
- ✅ No query string synchronization
- ✅ Cleaner clear/reset logic

#### 4. Improved Scroll Restoration

- ✅ Enhanced [scroll.js](src/web/Jordnaer/wwwroot/js/scroll.js)
- ✅ New `userSearchScroll` module
- ✅ Tracks most visible element (not just pixel position)
- ✅ Uses `requestAnimationFrame` for proper timing
- ✅ More reliable across screen sizes
- ✅ Fallback to pixel position if element restoration fails

#### 5. Browser Geolocation API

- ✅ Created [geolocation.js](src/web/Jordnaer/wwwroot/js/geolocation.js)
- ✅ Updated [MapSearchFilter.razor](src/web/Jordnaer/Features/Map/MapSearchFilter.razor)
- ✅ Auto-requests user location on load
- ✅ 5-second timeout with 5-minute cache
- ✅ Loading indicator while fetching
- ✅ Centers map on user's location if granted
- ✅ Graceful fallback to Denmark center
- ✅ Script loaded in [App.razor](src/web/Jordnaer/Components/App.razor)

### Files Modified/Created

**Created:**

- `src/web/Jordnaer/Features/UserSearch/UserCard.razor`
- `src/web/Jordnaer/Features/Ad/AdCard.razor`
- `src/web/Jordnaer/Features/Ad/HardcodedAds.cs`
- `src/web/Jordnaer/wwwroot/js/geolocation.js`

**Modified:**

- `src/web/Jordnaer/Pages/UserSearch/UserSearch.razor` - Simplified, removed query strings
- `src/web/Jordnaer/Features/UserSearch/UserSearchForm.razor` - Removed URL state
- `src/web/Jordnaer/Features/UserSearch/UserSearchResultComponent.razor` - New card layout + ads
- `src/web/Jordnaer/Features/Map/MapSearchFilter.razor` - Geolocation support
- `src/web/Jordnaer/wwwroot/js/scroll.js` - Better scroll restoration
- `src/web/Jordnaer/Components/App.razor` - Added geolocation.js script

---

## 🔲 TODO - Group Search Improvements

### Objective

Apply the same improvements to group search that were done for user search.

**⚠️ IMPORTANT:** User search improvements (above) were completed first to establish the pattern. Review the completed user search implementation before starting group search to understand the approach.

### Current State

[GroupSearch.razor](src/web/Jordnaer/Pages/GroupSearch/GroupSearch.razor) still has:

- ❌ Query string integration
- ❌ Auto-search on navigation
- ❌ Separate SponsorAd component (not integrated)
- ❌ Uses old [GroupCard.razor](src/web/Jordnaer/Features/GroupSearch/GroupCard.razor) pattern

### Tasks to Complete

#### 1. Simplify GroupSearch.razor

**Remove query string integration:**

```csharp
// DELETE these [SupplyParameterFromQuery] parameters:
[SupplyParameterFromQuery]
public string? Name { get; set; }
// ... all other query parameters

// DELETE these methods:
private async Task UpdateQueryString()
private async ValueTask LoadFromQueryString()

// CHANGE OnInitializedAsync:
protected override void OnInitialized()
{
    // Restore from cache if available
    if (Cache.SearchFilter is not null && Cache.SearchResult is not null)
    {
        _filter = Cache.SearchFilter;
        _searchResult = Cache.SearchResult;
        _hasSearched = true;
    }
}
```

**Remove SponsorAd above results:**

```razor
<!-- DELETE this entire section: -->
<SponsorAd Class="mt-5"
           ImageAlt="Reklame for Moon Creative"
           MobileImagePath="@Assets["images/ads/mooncreative_mobile.png"]"
           DesktopImagePath="@Assets["images/ads/mooncreative_mobile.png"]"
           Link="https://www.mooncreative.dk/" />
```

**Update Search method:**

```csharp
private async Task Search()
{
    _isSearching = true;

    _searchResult = await GroupSearchService.GetGroupsAsync(_filter);

    Snackbar.Clear();

    if (_searchResult.TotalCount is 0)
    {
        Snackbar.Add(/* ... */);
    }
    else
    {
        Snackbar.Add(/* ... */);
    }

    _hasSearched = true;
    _isSearching = false;

    // Save to cache for restoration when navigating back
    Cache.SearchFilter = _filter;
    Cache.SearchResult = _searchResult;
}
```

#### 2. Update GroupSearchResultComponent.razor

Follow the same pattern as UserSearchResultComponent:

**Add ad integration:**

```csharp
@using Jordnaer.Features.Ad

private record SearchResultItem
{
    public bool IsAd { get; init; }
    public GroupSlim? Group { get; init; }
    public AdData? Ad { get; init; }
}

private List<SearchResultItem> GetItemsWithAds()
{
    var items = new List<SearchResultItem>();

    // Calculate how many ads we need based on results
    // Always show at least 1 ad if there are any results
    var adCount = SearchResult.Groups.Count > 0
        ? Math.Max(1, (int)Math.Ceiling(SearchResult.Groups.Count / 8.0))
        : 0;

    var ads = HardcodedAds.GetAdsForUserSearch(adCount);
    var adIndex = 0;

    // Add groups with ads interspersed
    for (int i = 0; i < SearchResult.Groups.Count; i++)
    {
        items.Add(new SearchResultItem { Group = SearchResult.Groups[i] });

        // Insert ad after every 8th group, or at the end if we have fewer than 8 groups
        var shouldInsertAd = (i + 1) % 8 == 0 || (i == SearchResult.Groups.Count - 1 && adIndex < ads.Count);

        if (shouldInsertAd && adIndex < ads.Count)
        {
            items.Add(new SearchResultItem
            {
                IsAd = true,
                Ad = ads[adIndex++]
            });
        }
    }

    return items;
}
```

**Update rendering:**

```razor
<MudGrid Spacing="3">
    @{
        var items = GetItemsWithAds();
        var index = 0;
    }
    @foreach (var item in items)
    {
        var itemId = GetItemId(index);
        <MudItem xs="12" sm="6" md="4" lg="3" id="@itemId">
            @if (item.IsAd && item.Ad is not null)
            {
                <AdCard Link="@item.Ad.Link"
                        ImagePath="@item.Ad.ImagePath"
                        ImageAlt="@item.Ad.Title"
                        Title="@item.Ad.Title"
                        Description="@item.Ad.Description" />
            }
            else if (item.Group is not null)
            {
                <MudNavLink Href="@($"/groups/{item.Group.Id}")" Class="card-link">
                    <GroupCard Group="@item.Group" />
                </MudNavLink>
            }
        </MudItem>
        index++;
    }
</MudGrid>
```

**Add scroll restoration:**

```csharp
@inject IJSRuntime JsRuntime
@inject NavigationManager Navigation
@implements IDisposable

private IDisposable? _locationChangingHandler;
private string GetItemId(int index) => $"group-search-item-{index}";

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await RestoreScrollPosition();
    }
}

private async Task RestoreScrollPosition()
{
    await JsRuntime.InvokeVoidAsyncWithErrorHandling("groupSearchScroll.restoreScrollPosition");
}

private async Task SaveScrollPosition()
{
    await JsRuntime.InvokeVoidAsyncWithErrorHandling("groupSearchScroll.saveScrollPosition");
}

protected override void OnInitialized()
{
    _locationChangingHandler = Navigation
        .RegisterLocationChangingHandler(async _ => await SaveScrollPosition());
}

public void Dispose()
{
    _locationChangingHandler?.Dispose();
}
```

#### 3. Add Group Search Scroll Restoration to scroll.js

Add to [scroll.js](src/web/Jordnaer/wwwroot/js/scroll.js):

```javascript
// Group search scroll restoration (same pattern as user search)
window.groupSearchScroll = {
  saveScrollPosition: function () {
    const scrollY = window.scrollY;
    if (scrollY === 0) {
      sessionStorage.removeItem("groupSearch:scrollY");
      sessionStorage.removeItem("groupSearch:visibleItemId");
      return;
    }

    sessionStorage.setItem("groupSearch:scrollY", scrollY.toString());

    const items = document.querySelectorAll('[id^="group-search-item-"]');
    if (items.length === 0) {
      return;
    }

    let mostVisibleItem = null;
    let maxVisibleArea = 0;

    items.forEach((item) => {
      const rect = item.getBoundingClientRect();
      const viewportHeight = window.innerHeight;
      const visibleTop = Math.max(0, rect.top);
      const visibleBottom = Math.min(viewportHeight, rect.bottom);
      const visibleHeight = Math.max(0, visibleBottom - visibleTop);
      const visibleArea = visibleHeight * rect.width;

      if (visibleArea > maxVisibleArea) {
        maxVisibleArea = visibleArea;
        mostVisibleItem = item;
      }
    });

    if (mostVisibleItem) {
      sessionStorage.setItem("groupSearch:visibleItemId", mostVisibleItem.id);
    }
  },

  restoreScrollPosition: function () {
    const visibleItemId = sessionStorage.getItem("groupSearch:visibleItemId");
    const scrollY = sessionStorage.getItem("groupSearch:scrollY");

    if (!visibleItemId && !scrollY) {
      return;
    }

    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        let restored = false;

        if (visibleItemId) {
          const element = document.getElementById(visibleItemId);
          if (element) {
            element.scrollIntoView({ behavior: "instant", block: "start" });
            restored = true;
          }
        }

        if (!restored && scrollY) {
          window.scrollTo({
            top: parseInt(scrollY, 10),
            left: 0,
            behavior: "instant",
          });
        }
      });
    });
  },
};
```

#### 4. Update GroupSearchForm.razor (if needed)

Check if GroupSearchForm has similar query string logic to UserSearchForm. If so, remove it using the same pattern.

#### 5. Update GroupCard.razor Layout (Optional)

Consider updating [GroupCard.razor](src/web/Jordnaer/Features/GroupSearch/GroupCard.razor) to match the modern style of UserCard if needed. Current GroupCard looks good, but could benefit from:

- Consistent hover effects
- Better spacing
- Same elevation/shadow pattern

---

## Acceptance Criteria - Group Search

### Simplified Search Experience

- [ ] Query string parameters removed from GroupSearch.razor
- [ ] Auto-search on navigation removed
- [ ] Uses component-level state only
- [ ] Cache restores filter and results on back navigation

### Ad Integration

- [ ] Ads integrated within results grid (not separate section)
- [ ] Uses HardcodedAds.GetAdsForUserSearch()
- [ ] Shows at least 1 ad if there are results
- [ ] Ads appear every 8 groups or at end
- [ ] Uses AdCard component

### Scroll Restoration

- [ ] groupSearchScroll added to scroll.js
- [ ] Saves visible item ID + scroll position
- [ ] Restores to visible element on back navigation
- [ ] Implements IDisposable for cleanup

### Consistency

- [ ] GroupCard updated to match UserCard style (if needed)
- [ ] Same responsive grid layout
- [ ] Same hover effects
- [ ] Consistent with JordnaerPalette design system

---

## Files to Modify - Group Search

**Modify:**

- `src/web/Jordnaer/Pages/GroupSearch/GroupSearch.razor` - Remove query strings, simplify
- `src/web/Jordnaer/Features/GroupSearch/GroupSearchResultComponent.razor` - Add ads, scroll restoration
- `src/web/Jordnaer/wwwroot/js/scroll.js` - Add groupSearchScroll
- `src/web/Jordnaer/Features/GroupSearch/GroupCard.razor` - Update styling (optional)
- `src/web/Jordnaer/Features/GroupSearch/GroupSearchForm.razor` - Remove query strings (if applicable)

---

## Notes for Next Agent

### Pattern to Follow

User search has been fully updated. Use it as a reference:

1. Look at [UserSearch.razor](src/web/Jordnaer/Pages/UserSearch/UserSearch.razor) for simplified search page
2. Look at [UserSearchResultComponent.razor](src/web/Jordnaer/Features/UserSearch/UserSearchResultComponent.razor) for ad integration
3. Look at [scroll.js](src/web/Jordnaer/wwwroot/js/scroll.js) for userSearchScroll pattern

### Key Points

- **Don't over-engineer** - Copy the exact pattern from user search
- **Reuse HardcodedAds** - Same ad data source for both searches
- **Same scroll restoration pattern** - Just change "user" to "group" in identifiers
- **Test with 1, 7, 8, 9, and 16 results** - Verify ad placement works correctly

### Migration Path

These changes prepare for **Task 03 (Advertisement Management System)**. When that's implemented, only one line needs to change:

```csharp
// From:
var ads = HardcodedAds.GetAdsForUserSearch(adCount);

// To:
var ads = await AdService.GetActiveAdsAsync(AdPlacement.GroupSearch, adCount);
```

---

## Estimated Effort - Group Search

**Total: 1-2 hours**

- Update GroupSearch.razor: 20 min
- Update GroupSearchResultComponent.razor: 30 min
- Add groupSearchScroll to scroll.js: 10 min
- Test and verify: 20-40 min
- Optional GroupCard styling: 20 min

---

## Success Metrics

**User Search (Completed):**

- ✅ Modern, scannable card layout
- ✅ Ads integrated naturally
- ✅ No deployments needed for search changes
- ✅ Reliable scroll restoration
- ✅ Geolocation support

**Group Search (Next):**

- 🔲 Consistency with user search
- 🔲 Same ad integration pattern
- 🔲 Same simplified state management
- 🔲 Same scroll restoration quality

## 🔲 OUTSTANDING ISSUES - User Search Polish

These refinements need to be addressed for the user search experience:

### 1. Map Search Radius Configuration
**Issue:** The radius slider currently goes from 1-50km with a default of 10km.

**Required Changes:**
- Update [MapSearchFilter.razor](src/web/Jordnaer/Features/Map/MapSearchFilter.razor)
- Change Min from `1` to `1` (keep same)
- Change Max from `50` to `500`
- Change default RadiusKm from `10` to `25`
- Update the slider step if needed for better UX at higher values

```razor
<MudSlider Value="@RadiusKm" Min="1" Max="500" Step="5" Color="Color.Primary" ... />
```

### 2. Card Layout Issues - Missing Data Handling
**Problem:** When users are missing data (no categories or no children), the cards look messy and unbalanced. The layout becomes inconsistent across the grid.

**Root Cause:**
- Cards with full data (children + categories) are taller
- Cards with partial data appear shorter and unbalanced
- The `flex-grow-1` on categories section causes uneven spacing

**Solution Needed:**
- Ensure all cards have consistent height regardless of content
- The MudPaper already uses `height: 100%` - verify this is working
- Consider adding minimum height constraints
- Ensure the categories section at the bottom properly fills remaining space
- Test with various combinations:
  - User with children + categories
  - User with only children
  - User with only categories
  - User with neither

### 3. Ad Placement Logic - Less Than 8 Results
**Current Behavior:** When there are fewer than 8 results, the ad is always placed at the end of the list.

**Required Behavior:**
- If results count < 8, place the ad at a random position within the results
- Example: With 5 results, ad could appear at position 1, 2, 3, 4, or 5 (randomly chosen)
- This makes the ad placement less predictable and more natural

**Files to Update:**
- [UserSearchResultComponent.razor](src/web/Jordnaer/Features/UserSearch/UserSearchResultComponent.razor) - `GetItemsWithAds()` method
- [GroupSearchResultComponent.razor](src/web/Jordnaer/Features/GroupSearch/GroupSearchResultComponent.razor) - `GetItemsWithAds()` method

**Implementation:**
```csharp
private List<SearchResultItem> GetItemsWithAds()
{
    var items = new List<SearchResultItem>();
    var adCount = SearchResult.Users.Count > 0 ? Math.Max(1, (int)Math.Ceiling(SearchResult.Users.Count / 8.0)) : 0;
    var ads = HardcodedAds.GetAdsForUserSearch(adCount);

    if (SearchResult.Users.Count < 8 && ads.Count > 0)
    {
        // Random placement for small result sets
        var random = new Random();
        var adPosition = random.Next(0, SearchResult.Users.Count + 1);

        for (int i = 0; i <= SearchResult.Users.Count; i++)
        {
            if (i == adPosition)
            {
                items.Add(new SearchResultItem { IsAd = true, Ad = ads[0] });
            }
            if (i < SearchResult.Users.Count)
            {
                items.Add(new SearchResultItem { User = SearchResult.Users[i] });
            }
        }
    }
    else
    {
        // Regular pattern: every 8 items
        // ... existing logic
    }

    return items;
}
```

### 4. Single Ad Per Page Limit
**Current Behavior:** The current logic calculates ads based on total results: `Math.Max(1, (int)Math.Ceiling(SearchResult.Users.Count / 8.0))`. This could show multiple ads on one pagination page.

**Required Behavior:**
- Never display more than 1 ad per pagination page
- Even if there are 20+ results on a page, only show 1 ad
- This keeps the ad-to-content ratio reasonable

**Files to Update:**
- [UserSearchResultComponent.razor](src/web/Jordnaer/Features/UserSearch/UserSearchResultComponent.razor)
- [GroupSearchResultComponent.razor](src/web/Jordnaer/Features/GroupSearch/GroupSearchResultComponent.razor)

**Implementation:**
```csharp
private List<SearchResultItem> GetItemsWithAds()
{
    var items = new List<SearchResultItem>();

    // Always max 1 ad per page, regardless of result count
    var adCount = SearchResult.Users.Count > 0 ? 1 : 0;
    var ads = HardcodedAds.GetAdsForUserSearch(adCount);

    // ... rest of logic
}
```

### 5. Chip Color Palette - Children & Categories
**Problem:** The current colors for children (Primary/blue) and categories (Tertiary) don't match the warm, friendly UI of Jordnaer.

**Current Colors:**
- Children chips: `Color.Primary` (blue)
- Category chips: `Color.Tertiary` (outlined)

**Required Changes:**
Check [JordnaerPalette.cs](src/web/Jordnaer/Shared/JordnaerPalette.cs) for the color scheme and update:

**Files to Update:**
- [UserCard.razor](src/web/Jordnaer/Features/UserSearch/UserCard.razor)

**Suggested Approach:**
- Use softer, warmer colors from the Jordnaer palette
- Children chips could use a soft pastel color (not blue)
- Category chips could use outlined style with the warm theme colors
- Consider using `Style` parameter with custom colors from JordnaerPalette
- Example: `Style="background-color: {JordnaerPalette.LightWarmColor};"`

**Test Cases:**
- Verify colors work in both light and dark themes (if supported)
- Ensure sufficient contrast for accessibility
- Check that colors are visually distinct between children and categories

---

## Already Completed Fixes (Don't Redo)

✅ **Map Clear Button** - Fixed JS/Blazor desync by using `LeafletMap.IsInitialized` property
✅ **Location/Username Colors** - Changed to subtle blue (#5B9BD5) instead of red
✅ **Children Section Icon** - Removed ChildCare emoji, using clean text label
✅ **Card Background** - Fixed grey background issue with explicit `background-color: white`
✅ **AdCard Annonce Label** - Styled as small, muted, upper right corner with grey background
✅ **Single Result Centering** - Added `Justify.Center` when results count ≤ 3
✅ **Search Form Spacing** - Added `mt-5` margin between form and results
✅ **Divider Colors** - Set to `#e0e0e0` with `opacity: 0.5`
