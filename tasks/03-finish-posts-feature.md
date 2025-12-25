# Task 05: Improve /Posts Feature UX

## Context
**App:** Jordnaer (.NET Blazor Server)
**Area:** Posts
**Priority:** High

## Objective
Improve the UX of the /Posts feature to create a polished, Facebook-like experience focused on content consumption with minimalist creation UI.

## Current State - What's Already Done ✅

The following features have been implemented and are working:
- ✅ User avatars in PostCardComponent
- ✅ Delete functionality with authorization (PostService.DeletePostAsync)
- ✅ WYSIWYG rich text editor in CreatePostComponent
- ✅ Fixed pagination calculation (Math.Ceiling)
- ✅ Post detail page at `/posts/{id}`
- ✅ Edit functionality at `/posts/{id}/edit`
- ✅ Edit button in PostCardComponent (currently visible icon)
- ✅ Category display as chips
- ✅ OnPostCreated/OnPostDeleted callbacks wired up
- ✅ Basic search functionality with filters

## Problems to Fix

### 1. **Reading vs Writing Balance** 🎯
**Problem**: The create post form is prominent, but 95% of users come to read, not write.
**Solution**: Make the create form minimal and collapsible, prioritizing content display.

### 2. **No Default Content Feed** 🎯
**Problem**: Page is blank until users search - not user-friendly.
**Solution**: Show 5-10 most recent posts by default, sorted by date (descending).

### 3. **Search is Not the Golden Path** 🎯
**Problem**: Users shouldn't need to search to see posts.
**Solution**: Recent posts should be the default view with cursor pagination for browsing history.

### 4. **Post Actions UI Needs Polish** 🎯
**Problem**: Colored edit/delete icons look unpolished.
**Solution**: Hide edit/delete behind a 3-dot menu (⋮) for cleaner appearance.

### 5. **Floating Text Layout** 🎯
**Problem**: Posts are just fluid text without clear sections - lacks structure.
**Solution**: Better visual hierarchy with clear sections, spacing, and containers.

### 6. **Category Badge Styling** 🎯
**Problem**: Category chips look off and don't match design system.
**Solution**: Use warmer colors from `DESIGN_SYSTEM.md` (GLÆDE yellow, RO green, etc.)

### 7. **HTML Sanitization** 🔒
**Problem**: Need to ensure all user-created HTML is sanitized.
**Status**: Currently using `.SanitizeHtml()` extension - verify it's applied everywhere.

### 8. **Ad Integration** 💰
**Problem**: No ads integrated in the posts feed - missing monetization opportunity.
**Solution**: Integrate `SponsorAd` component cleanly within the post feed, similar to UserSearch.

## Detailed Requirements

### Requirement 1: Minimal Create Post UI

**Inspiration**: Facebook's "What's on your mind?" collapsed input

**Current State**: Full form with WYSIWYG editor always visible
**Target State**: Collapsed input that expands on click

**Implementation**:
```razor
<!-- Collapsed State (Default) -->
<MudCard Class="mb-4" Style="cursor: pointer;" @onclick="ExpandCreateForm">
  <MudCardContent Class="pa-3">
    <MudStack Row AlignItems="AlignItems.Center" Spacing="3">
      <MudAvatar Color="Color.Secondary">
        @* User avatar *@
      </MudAvatar>
      <MudTextField Placeholder="Hvad tænker du på?"
                    Variant="Variant.Outlined"
                    ReadOnly
                    FullWidth
                    Class="warm-rounded" />
    </MudStack>
  </MudCardContent>
</MudCard>

<!-- Expanded State (On Click) -->
@if (_showCreateForm)
{
  <MudCard Class="mb-4">
    <MudCardContent>
      @* Full WYSIWYG editor and form here *@
    </MudCardContent>
  </MudCard>
}
```

**Files to Modify**:
- `src/web/Jordnaer/Features/Posts/CreatePostComponent.razor`
- Add `_isExpanded` state to toggle between minimal/full view

### Requirement 2: Default Recent Posts Feed

**Current State**: Empty page until search is performed
**Target State**: Show 5-10 most recent posts on page load

**Implementation**:
```csharp
protected override async Task OnInitializedAsync()
{
    var userProfile = await ProfileCache.GetProfileAsync();
    _currentUserId = userProfile?.Id;

    // NEW: Load recent posts by default
    await LoadRecentPosts();

    // Load from query string if present (search)
    if (HasQueryStringParameters())
    {
        await LoadFromQueryString();
    }
}

private async Task LoadRecentPosts()
{
    _isLoadingRecent = true;
    var result = await PostSearchService.GetRecentPostsAsync(pageSize: 10);
    // ... handle result
    _isLoadingRecent = false;
}
```

**Files to Modify**:
- `src/web/Jordnaer/Pages/Posts/Posts.razor`
- `src/web/Jordnaer/Features/PostSearch/PostSearchService.cs` - Add `GetRecentPostsAsync` method

### Requirement 3: Cursor-Based Pagination for Recent Posts

**Current State**: Offset pagination with page numbers
**Target State**: "Load More" button with cursor-based pagination

**Why Cursor Pagination?**:
- Better performance for large datasets
- No duplicate/missing items when new posts are added
- Simpler UX than numbered pages for a feed

**Implementation Pattern**:
```csharp
public class PostSearchResult
{
    public List<PostDto> Posts { get; set; }
    public string? NextCursor { get; set; } // NEW: Cursor for next page
    public bool HasMore { get; set; } // NEW: Flag for more results
    public int TotalCount { get; set; }
}

// Service method
public async Task<PostSearchResult> GetRecentPostsAsync(
    int pageSize = 10,
    string? cursor = null)
{
    // Use CreatedUtc as cursor
    // Query: WHERE CreatedUtc < cursor ORDER BY CreatedUtc DESC LIMIT pageSize + 1
    // If results.Count > pageSize, set NextCursor = last.CreatedUtc
}
```

**UI Component**:
```razor
@if (_recentPosts.HasMore)
{
    <div class="d-flex justify-center my-4">
        <MudButton Variant="Variant.Outlined"
                   OnClick="LoadMorePosts"
                   Disabled="_isLoadingMore">
            @if (_isLoadingMore)
            {
                <MudProgressCircular Size="Size.Small" Indeterminate />
                <MudText Class="ml-2">Indlæser...</MudText>
            }
            else
            {
                <MudText>Vis flere opslag</MudText>
            }
        </MudButton>
    </div>
}
```

**Files to Modify**:
- `src/shared/Jordnaer.Shared/Posts/PostSearchResult.cs` - Add cursor fields
- `src/web/Jordnaer/Features/PostSearch/PostSearchService.cs` - Implement cursor logic
- `src/web/Jordnaer/Pages/Posts/Posts.razor` - Add "Load More" UI

### Requirement 4: Three-Dot Menu for Post Actions

**Current State**: Colored edit/delete icon buttons visible to post owner
**Target State**: Three-dot menu (⋮) with edit/delete options

**Implementation**:
```razor
<CardHeaderActions>
    @if (PostItem.Author.Id == CurrentUserId)
    {
        <MudMenu Icon="@Icons.Material.Filled.MoreVert"
                 AnchorOrigin="Origin.BottomRight"
                 Dense="true"
                 @onclick:stopPropagation="true">
            <MudMenuItem Icon="@Icons.Material.Filled.Edit"
                         Href="@($"/posts/{PostItem.Id}/edit")">
                Redigér
            </MudMenuItem>
            <MudMenuItem Icon="@Icons.Material.Filled.Delete"
                         OnClick="DeletePostAsync"
                         IconColor="Color.Error">
                Slet
            </MudMenuItem>
        </MudMenu>
    }
</CardHeaderActions>
```

**Files to Modify**:
- `src/web/Jordnaer/Features/Posts/PostCardComponent.razor` (lines 36-51)
- Remove the two separate MudIconButtons
- Replace with single MudMenu with MoreVert icon

### Requirement 5: Improved Post Layout Structure

**Current State**: Fluid text without clear sections
**Target State**: Well-defined sections with proper spacing

**Layout Structure**:
```razor
<MudCard Class="mb-4 warm-shadow warm-rounded">
    <!-- Header: Avatar, Name, Date, Menu -->
    <MudCardHeader Class="pb-0">
        <CardHeaderAvatar>@* Avatar *@</CardHeaderAvatar>
        <CardHeaderContent>
            @* Name, timestamp, location *@
        </CardHeaderContent>
        <CardHeaderActions>@* Three-dot menu *@</CardHeaderActions>
    </MudCardHeader>

    <!-- Content: Post text -->
    <MudCardContent Class="py-3">
        <MudText Typo="Typo.body1" Class="mb-3"
                 Style="@($"color: {JordnaerPalette.BlueBody}; line-height: 1.6;")">
            @PostItem.Text.SanitizeHtml()
        </MudText>

        <!-- Categories -->
        @if (PostItem.Categories.Any())
        {
            <MudStack Row Spacing="1" Class="mt-2">
                @foreach (var category in PostItem.Categories)
                {
                    <MudChip T="string"
                             Size="Size.Small"
                             Style="@GetCategoryStyle()">
                        @category
                    </MudChip>
                }
            </MudStack>
        }
    </MudCardContent>

    <!-- Footer: Interactions (if added later) -->
    <MudCardActions Class="pt-0">
        @* Future: Like, Comment, Share buttons *@
    </MudCardActions>
</MudCard>
```

**Design System Integration**:
```csharp
@using Jordnaer.Features.Theme

private string GetCategoryStyle()
{
    // Rotate through warm brand colors
    return $"background-color: {JordnaerPalette.YellowBackground}; color: white; font-weight: 500;";
    // or: JordnaerPalette.GreenBackground
    // or: JordnaerPalette.BeigeBackground with dark text
}
```

**Files to Modify**:
- `src/web/Jordnaer/Features/Posts/PostCardComponent.razor`
- Add `@using Jordnaer.Features.Theme`
- Apply `.warm-shadow`, `.warm-rounded` classes
- Use `JordnaerPalette` colors for text and backgrounds

### Requirement 6: Warm Category Badges

**Current State**: Generic MudChip with primary color
**Target State**: Warm, brand-aligned category badges

**Color Recommendations** (from `DESIGN_SYSTEM.md`):
- **GLÆDE (Yellow)**: `#dbab45` - Primary brand color
- **RO (Green)**: `#878e64` - Secondary, calming
- **OMSORG (Beige)**: `#cfc1a6` - Softer alternative
- **LEG (Pale Blue)**: `#a9c0cf` - Light background

**Implementation**:
```razor
@foreach (var (category, index) in PostItem.Categories.Select((c, i) => (c, i)))
{
    <MudChip T="string"
             Size="Size.Small"
             Class="warm-rounded"
             Style="@GetCategoryChipStyle(index)">
        @category
    </MudChip>
}

@code {
    private string GetCategoryChipStyle(int index)
    {
        // Rotate through brand colors
        var colors = new[]
        {
            (bg: JordnaerPalette.YellowBackground, fg: "white"),
            (bg: JordnaerPalette.GreenBackground, fg: "white"),
            (bg: JordnaerPalette.BeigeBackground, fg: JordnaerPalette.BlueBody)
        };

        var color = colors[index % colors.Length];
        return $"background-color: {color.bg}; color: {color.fg}; font-weight: 500; border: none;";
    }
}
```

**Files to Modify**:
- `src/web/Jordnaer/Features/Posts/PostCardComponent.razor` (lines 54-60)

### Requirement 7: HTML Sanitization Audit

**Current State**: Using `.SanitizeHtml()` in PostCardComponent
**Verify**: All user HTML is sanitized in all display contexts

**Files to Check**:
- ✅ `PostCardComponent.razor` - Uses `.SanitizeHtml()` (line 53)
- ⚠️ `PostDetail.razor` - Check if it reuses PostCardComponent (should be safe)
- ⚠️ `EditPost.razor` - Check if preview uses sanitization
- ✅ `GroupPostCardComponent.razor` - Already uses `.SanitizeHtml()` (line 39)

**Extension Method Location**:
- `src/web/Jordnaer/Features/Profile/MarkdownRenderer.cs`
- Uses `HtmlSanitizer` from Ganss.Xss library

**No Changes Needed** - Just verify all render points use the extension.

### Requirement 8: Clean Ad Integration

**Current State**: SponsorAd shown above search results (line 37 in Posts.razor)
**Target State**: Ads integrated naturally within the post feed

**Pattern from UserSearch.razor**:
```razor
<SponsorAd Class="mt-5"
           ImageAlt="Reklame for Moon Creative"
           MobileImagePath="@Assets["images/ads/mooncreative_mobile.png"]"
           DesktopImagePath="@Assets["images/ads/mooncreative_mobile.png"]"
           Link="https://www.mooncreative.dk/" />
```

**Implementation Options**:

**Option A: Between Recent Posts and Search** (Recommended)
```razor
<!-- Recent Posts Feed -->
<RecentPostsComponent />

<!-- Ad Placement -->
<SponsorAd Class="my-5"
           ImageAlt="Reklame for Moon Creative"
           MobileImagePath="images/ads/mooncreative_mobile.png"
           DesktopImagePath="images/ads/mooncreative_mobile.png"
           Link="https://www.mooncreative.dk/" />

<!-- Search Section (Collapsible) -->
<PostSearchForm />
```

**Option B: Inline in Feed (Every 5th Post)**
```razor
@foreach (var (post, index) in _recentPosts.Posts.Select((p, i) => (p, i)))
{
    <PostCardComponent PostItem="post" ... />

    @if ((index + 1) % 5 == 0 && index < _recentPosts.Posts.Count - 1)
    {
        <!-- Ad after every 5 posts -->
        <SponsorAd Class="my-4"
                   ImageAlt="Sponsor reklame"
                   MobileImagePath="images/ads/mooncreative_mobile.png"
                   DesktopImagePath="images/ads/mooncreative_mobile.png"
                   Link="https://www.mooncreative.dk/" />
    }
}
```

**Option C: Fixed Position After First 3 Posts** (Most Natural)
```razor
@foreach (var (post, index) in _recentPosts.Posts.Select((p, i) => (p, i)))
{
    <PostCardComponent PostItem="post" ... />

    @if (index == 2) // After 3rd post (0-indexed)
    {
        <SponsorAd Class="my-5"
                   ImageAlt="Sponsor reklame"
                   MobileImagePath="images/ads/mooncreative_mobile.png"
                   DesktopImagePath="images/ads/mooncreative_mobile.png"
                   Link="https://www.mooncreative.dk/" />
    }
}
```

**Recommendation**: Use **Option C** for recent posts feed
- Shows ad after 3 posts (visible without scroll on most screens)
- Doesn't interrupt the reading flow too early
- Clean, predictable placement
- Matches Facebook/Instagram feed patterns

**For Search Results**: Keep current placement (above results) or use Option B

**Ad Metrics**: The `SponsorAd` component automatically tracks views via `JordnaerMetrics.SponsorAdViewCounter`

**Files to Modify**:
- `src/web/Jordnaer/Features/Posts/PostSearchResultComponent.razor` (for inline ads in search results)
- `src/web/Jordnaer/Pages/Posts/Posts.razor` (for feed ads in recent posts)

**Design Considerations**:
- Ad should have visual breathing room (`my-5` spacing)
- Should be clearly labeled as "Sponsor reklame" (already in component)
- Responsive sizing handled by SponsorAd component
- Lazy loading enabled for performance

## UI/UX Flow

### Default View (No Search)
```
┌─────────────────────────────────────┐
│  [Avatar] "Hvad tænker du på?"      │ <- Minimal create form
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│  Recent Posts (Auto-loaded)         │
│  ├─ Post 1                          │
│  ├─ Post 2                          │
│  ├─ Post 3                          │
│  ├─ [SPONSOR AD]                    │ <- Ad after 3rd post
│  ├─ Post 4                          │
│  ├─ Post 5                          │
│  └─ ...                             │
│  [Vis flere opslag] <- Load More    │
└─────────────────────────────────────┘

[Toggle Search Filters] <- Collapsible
```

### Search View
```
┌─────────────────────────────────────┐
│  [Search Filters Expanded]          │
│  ├─ Text search                     │
│  ├─ Categories                      │
│  └─ Location                        │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│  Search Results                     │
│  ├─ Matching posts                  │
│  └─ Traditional pagination          │
└─────────────────────────────────────┘
```

## Implementation Order

1. **Three-Dot Menu** (Quick Win)
   - Replace edit/delete icons with MudMenu
   - File: `PostCardComponent.razor`

2. **Category Badge Styling** (Quick Win)
   - Apply warm colors from design system
   - File: `PostCardComponent.razor`

3. **Improved Post Layout** (Medium)
   - Better spacing, sections, design system colors
   - File: `PostCardComponent.razor`

4. **Default Recent Posts** (Medium)
   - Add `GetRecentPostsAsync` to service
   - Load on page init
   - Files: `PostSearchService.cs`, `Posts.razor`

5. **Ad Integration in Feed** (Quick)
   - Add SponsorAd after 3rd post in feed
   - Files: `Posts.razor` or create `RecentPostsComponent.razor`

6. **Cursor Pagination** (Complex)
   - Modify `PostSearchResult` model
   - Implement cursor logic in service
   - Add "Load More" UI
   - Files: `PostSearchResult.cs`, `PostSearchService.cs`, `Posts.razor`

7. **Minimal Create Form** (Medium)
   - Add collapsed/expanded states
   - Rework CreatePostComponent UI
   - File: `CreatePostComponent.razor`

8. **Sanitization Audit** (Quick)
   - Verify all render points use `.SanitizeHtml()`
   - Files: All post display components

## Acceptance Criteria

- [ ] Create post form is minimal by default (collapsed input)
- [ ] 5-10 recent posts load automatically on page load
- [ ] Posts are sorted by date (newest first)
- [ ] "Load More" button loads next batch of posts
- [ ] Cursor pagination prevents duplicates/gaps
- [ ] Edit/delete hidden behind three-dot menu (⋮)
- [ ] Post cards have clear visual sections
- [ ] Category badges use warm brand colors
- [ ] All user HTML is sanitized (verified)
- [ ] SponsorAd integrated cleanly in feed (after 3rd post)
- [ ] Ad metrics tracked automatically
- [ ] Search is available but not required
- [ ] Mobile and desktop layouts work well
- [ ] Design system colors applied correctly

## Files to Modify

**Priority 1 (Quick Wins)**:
- `src/web/Jordnaer/Features/Posts/PostCardComponent.razor`

**Priority 2 (Core Features)**:
- `src/web/Jordnaer/Pages/Posts/Posts.razor`
- `src/web/Jordnaer/Features/Posts/CreatePostComponent.razor`
- `src/web/Jordnaer/Features/PostSearch/PostSearchService.cs`

**Priority 3 (Data Models)**:
- `src/shared/Jordnaer.Shared/Posts/PostSearchResult.cs`

## Design System References

From `docs/DESIGN_SYSTEM.md`:
- Use `JordnaerPalette.YellowBackground` for primary elements
- Use `JordnaerPalette.GreenBackground` for success/secondary
- Use `JordnaerPalette.BlueBody` for readable text
- Apply `.warm-shadow`, `.warm-rounded` utility classes
- Maintain decent contrast for accessibility

## Testing Checklist

- [ ] Create post form expands/collapses smoothly
- [ ] Recent posts load on initial page visit
- [ ] Load More button loads next 10 posts
- [ ] Cursor pagination works correctly (no duplicates)
- [ ] Three-dot menu shows edit/delete options
- [ ] Only post author sees three-dot menu
- [ ] Delete confirmation works
- [ ] Category badges render with warm colors
- [ ] All user HTML is sanitized
- [ ] SponsorAd appears after 3rd post in feed
- [ ] SponsorAd is responsive (mobile/desktop)
- [ ] Ad click tracking works (check metrics)
- [ ] Search still works when used
- [ ] Mobile layout is responsive
- [ ] Page performance is good with 50+ posts

## Notes

- **Inspiration**: Think Facebook's news feed - minimal input, content-focused
- **Golden Path**: User visits → sees recent posts → scrolls → loads more
- **Edge Case**: User searches → sees filtered results → uses traditional pagination
- **Performance**: Cursor pagination scales better than offset for large datasets
- **Polish**: Three-dot menu is more professional than colored action buttons
- **Monetization**: SponsorAd placement should feel natural, not intrusive
- **Ad Strategy**: After 3rd post = visible without scroll, doesn't interrupt initial engagement
- **Metrics**: SponsorAd component auto-tracks views via OpenTelemetry (JordnaerMetrics)

## Ad Integration Examples

**Good Ad Placement** (Natural, after 3 posts):
```
Post 1
Post 2
Post 3
[AD] ← User has seen content, ad feels earned
Post 4
Post 5
```

**Bad Ad Placement** (Too aggressive):
```
[AD] ← Immediate ad, no content first
Post 1
Post 2
```

**Current Implementation**:
- `Posts.razor` line 37: Ad shown above search results ✅
- **Need to add**: Ad in recent posts feed after 3rd post

**Component Reference**:
```razor
@using Jordnaer.Features.Ad

<SponsorAd Class="my-5"
           ImageAlt="Sponsor reklame"
           MobileImagePath="images/ads/mooncreative_mobile.png"
           DesktopImagePath="images/ads/mooncreative_mobile.png"
           Link="https://www.mooncreative.dk/" />
```

**SponsorAd Features**:
- Responsive (different images for mobile/desktop)
- Lazy loading for performance
- Automatic view tracking (OpenTelemetry metrics)
- Clear "Sponsor reklame" label for transparency
- Click tracking via metrics
