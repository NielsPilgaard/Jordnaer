# Task 05: Social Sharing (Facebook/Instagram)

## Context

**App:** Jordnaer (.NET Blazor Server)
**Area:** Groups & Posts
**Priority:** Medium

## Objective

Enable users to share groups and posts to Facebook and Instagram, increasing platform visibility and organic growth.

## Current State

- [GroupDetails.razor](src/web/Jordnaer/Pages/Groups/GroupDetails.razor) displays group info with `MetadataComponent` for SEO
- [PostDetail.razor](src/web/Jordnaer/Pages/Posts/PostDetail.razor) displays individual posts
- No social sharing functionality exists
- Open Graph meta tags partially implemented via `MetadataComponent`

## Requirements

### 1. Share Button Component

Create reusable `SocialShareButtons.razor`:

- Facebook Share button (uses Facebook Share Dialog)
- Instagram Share button (copy link with prompt, as Instagram lacks direct web share API)
- Copy Link button with clipboard feedback
- Native Web Share API fallback for mobile devices

### 2. Group Sharing

Add share buttons to [GroupDetails.razor](src/web/Jordnaer/Pages/Groups/GroupDetails.razor):

- Position in top action bar alongside existing buttons
- Share URL: `{BaseUrl}/groups/{GroupName}`
- Share text: Group name and short description

### 3. Post Sharing

Add share buttons to [PostDetail.razor](src/web/Jordnaer/Pages/Posts/PostDetail.razor):

- Position below or beside post content
- Share URL: `{BaseUrl}/posts/{PostId}`
- Share text: Post preview (first 100 chars)

### 4. Open Graph Meta Tags

Ensure `MetadataComponent` outputs proper OG tags for rich previews:

```html
<meta property="og:title" content="{Title}" />
<meta property="og:description" content="{Description}" />
<meta property="og:image" content="{ImageUrl}" />
<meta property="og:url" content="{CanonicalUrl}" />
<meta property="og:type" content="website" />
```

## Acceptance Criteria

- [ ] Share buttons visible on group detail page
- [ ] Share buttons visible on post detail page
- [ ] Facebook share opens share dialog with correct preview
- [ ] Instagram share copies link with snackbar confirmation
- [ ] Copy link button works with clipboard feedback
- [ ] Mobile devices use native share sheet when available
- [ ] Open Graph tags render correctly for link previews

## Files to Create/Modify

**Create:**
- `Components/SocialShareButtons.razor` - Reusable share button component

**Modify:**
- [GroupDetails.razor](src/web/Jordnaer/Pages/Groups/GroupDetails.razor) - Add share buttons
- [PostDetail.razor](src/web/Jordnaer/Pages/Posts/PostDetail.razor) - Add share buttons
- [MetadataComponent.razor](src/web/Jordnaer/Components/MetadataComponent.razor) - Ensure complete OG tags

## Technical Notes

- Facebook Share: `https://www.facebook.com/sharer/sharer.php?u={encodedUrl}`
- Use `navigator.share()` API for mobile with feature detection
- Instagram has no web share API; show "Copy link to share on Instagram" with clipboard copy
- URL encode all share parameters
