# Task 06: Create Groups Hub Page

## Context

**App:** Jordnaer (.NET Blazor Server)  
**Area:** Groups  
**Priority:** Medium

## Objective

Create a Groups hub/landing page that clearly compartmentalizes and makes discoverable the two main group experiences:

1. **Discover Groups** - Search and browse available groups to join
2. **My Groups** - Manage groups you own or are a member of

Think: Facebook's Groups landing page where users get a quick glance of their options.

## Current State

Groups functionality exists but the navigation between discovering new groups and managing existing groups is not well compartmentalized or discoverable.

## Design Vision

### Groups Hub Layout (Desktop)

```
┌─────────────────────────────────────────────────────┐
│  Groups                                              │
├─────────────────────────────────────────────────────┤
│                                                      │
│  ┌──────────────────┐  ┌──────────────────┐        │
│  │  Discover Groups │  │   My Groups      │        │
│  │                  │  │                  │        │
│  │  [Search icon]   │  │  [Groups icon]   │        │
│  │                  │  │                  │        │
│  │  Browse and join │  │  Groups you own  │        │
│  │  new groups      │  │  or are part of  │        │
│  │                  │  │                  │        │
│  │  [Explore →]     │  │  [View →]        │        │
│  └──────────────────┘  └──────────────────┘        │
│                                                      │
│  Quick Access:                                       │
│  ┌─────────────────────────────────────────┐       │
│  │ Recent Groups                            │       │
│  │ • Group Name 1    [View] [New Posts: 3] │       │
│  │ • Group Name 2    [View] [New Posts: 0] │       │
│  └─────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────┘
```

### Mobile: Stacked cards with clear CTAs

## Requirements

### 1. Groups Hub Page (`/Groups`)

- Create a landing page that acts as the entry point for all group activities
- Two prominent sections/cards:
  - **Discover Groups** → leads to group search/browse
  - **My Groups** → leads to owned and member groups
- Quick access section showing recent groups with activity indicators

### 2. Discover Groups Section

- Search functionality for finding groups
- Browse/filter options (by category, location, activity level)
- Clear "Join" CTAs on group cards
- Preview of group info before joining

### 3. My Groups Section

- Separate tabs/views for:
  - **Groups I Own** - with management options
  - **Groups I'm In** - with member options
- Quick actions per group (View, Post, Leave/Manage)
- Activity indicators (new posts, unread content)

### 4. Navigation & Discoverability

- Clear visual hierarchy between discovery and management
- Intuitive icons and labels
- Breadcrumb or back navigation from sub-pages
- Mobile-friendly navigation (bottom nav or hamburger menu)

## Acceptance Criteria

- [ ] Groups hub page created at `/Groups` route
- [ ] Two prominent sections: "Discover Groups" and "My Groups"
- [ ] "Discover Groups" leads to search/browse interface
- [ ] "My Groups" shows owned groups and member groups (separate tabs/sections)
- [ ] Quick access section shows recently active groups
- [ ] Activity indicators show new posts/content counts
- [ ] Responsive design (desktop: side-by-side cards, mobile: stacked)
- [ ] Clear CTAs and navigation paths
- [ ] Consistent with existing design system
- [ ] Fast load times (lazy load group lists if needed)

## User Flow Examples

### New User (No Groups Yet)

1. Lands on `/Groups` hub
2. Sees "Discover Groups" prominently
3. Clicks to browse/search
4. Finds and joins groups

### Active User (Multiple Groups)

1. Lands on `/Groups` hub
2. Sees "My Groups" with activity indicators
3. Clicks to view owned/member groups
4. Can also discover new groups from same hub

## Design Inspiration

- Facebook Groups landing page (compartmentalized discovery vs management)
- LinkedIn Groups (clear separation of "My Groups" vs "Discover")
- Discord server list (quick access to recent/active)

## Additional Requirements

- Create group needs address / zip code auto complete -> Point like in src\web\Jordnaer\Pages\Profile\MyProfile.razor, so we can do distance search

## Files to Investigate

- Current `/Groups` page component
- `/MyGroups` page component (may be merged into hub)
- `GroupCard.razor` (for displaying groups in lists)
- Group search/filter components
- Navigation/routing configuration
