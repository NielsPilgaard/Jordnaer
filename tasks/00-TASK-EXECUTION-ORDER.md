# Task Execution Order

This document explains the optimal order for executing the Jordnaer tasks, based on dependencies and minimizing duplicate work.

## Quick Reference

| Order | Task File | Task Name | Why This Order |
|-------|-----------|-----------|----------------|
| **1** | [01-warmer-ui-ux.md](01-warmer-ui-ux.md) | Warmer UI/UX | Foundation - establishes design system |
| **2** | [02-nettopologysuite-migration.md](02-nettopologysuite-migration.md) | NetTopologySuite | Backend infrastructure for map search |
| **3** | [03-finish-posts-feature.md](03-finish-posts-feature.md) | Finish Posts | Content ready for map search |
| **4** | [04-groups-navigation.md](04-groups-navigation.md) | Groups Hub | Content ready for map search |
| **5** | [05-map-search.md](05-map-search.md) | Map Search 🚩 | Complex feature - needs all prerequisites |
| **6** | [06-improve-user-search-display.md](06-improve-user-search-display.md) | User Search Display 🚩 | Benefits from map integration |
| **7** | [07-account-page-refresh.md](07-account-page-refresh.md) | Account Pages | Polish - isolated |
| **8** | [08-improve-chat-ui.md](08-improve-chat-ui.md) | Chat UI | Polish - independent |

🚩 = Behind `EnableNewSearchExperience` feature flag

---

## Phase Breakdown

### Phase 1: Foundation (Design System)

#### Task 01: Warmer UI/UX ⭐ **START HERE**
- **Priority:** Do this FIRST
- **Why:** Establishes color palette, typography, and design tokens that ALL other tasks will use
- **Impact:** Prevents rework - all subsequent UI changes use correct design system from the start
- **Quick Win:** Main fix is updating one color in `JordnaerPalette.cs` (#fcca3f → #dbab45)
- **Dependencies:** None
- **Enables:** All UI tasks (03, 04, 05, 06, 07, 08)

---

### Phase 2: Backend Infrastructure

#### Task 02: NetTopologySuite Migration
- **Priority:** Early backend work
- **Why:** Provides distance calculation engine needed for map search (Task 05)
- **No UI Dependency:** Pure backend work, won't conflict with design system changes
- **Dependencies:** None
- **Enables:** Task 05 (Map Search)
- **Key Deliverables:**
  - Distance calculation service using lat/long
  - Radius-based search queries
  - Privacy messaging in EditProfile
  - Zipcode-only option for privacy

---

### Phase 3: Core Features

#### Task 03: Finish Posts Feature
- **Priority:** Medium - needed before map search
- **Why:** Posts must be complete before they can appear in map search
- **Design System Ready:** Uses warmer UI/UX from Task 01
- **Dependencies:** Task 01 (design system)
- **Enables:** Task 05 (posts searchable on map)
- **Key Deliverables:**
  - Complete posts functionality
  - Filtering, sorting, pagination
  - Markdown rendering
  - Great-looking create post form

#### Task 04: Groups Navigation Hub
- **Priority:** Medium - needed before map search
- **Why:** Groups must be discoverable before they can appear in map search
- **Design System Ready:** Uses warmer UI/UX from Task 01
- **Dependencies:** Task 01 (design system)
- **Enables:** Task 05 (groups searchable on map)
- **Key Deliverables:**
  - Groups landing page at `/Groups`
  - "Discover Groups" and "My Groups" sections
  - Activity indicators
  - Quick access to recent groups

---

### Phase 4: Advanced Search (Feature Flagged)

#### Task 05: Map Search 🗺️ **FEATURE FLAG REQUIRED**
- **Priority:** High - complex feature
- **Why Now:** Backend ready (Task 02), content ready (Tasks 03, 04), design ready (Task 01)
- **Feature Flag:** `EnableNewSearchExperience = false` (start disabled)
- **Dependencies:** Tasks 01, 02, 03, 04
- **Impacts:** Task 06 (user search will integrate with map)
- **Scope:** Map search for **all three types**: Posts, Groups, Users
- **Key Deliverables:**
  - Leaflet.js map integration
  - Address search with DataForsyningen autocomplete
  - Radius selector (slider)
  - Map markers for posts, groups, users
  - Distance display from selected point
  - Works for all three search types

**Feature Flag Implementation:**
```csharp
// appsettings.json
{
  "FeatureFlags": {
    "EnableNewSearchExperience": false  // Toggle entire new search experience
  }
}
```

**When flag = false:** Existing search UI
**When flag = true:** New map-based search with improved display

#### Task 06: Improve User Search Display 🔍 **FEATURE FLAG REQUIRED**
- **Priority:** High - part of new search experience
- **Why Now:** Can design search results considering map integration from Task 05
- **Feature Flag:** `EnableNewSearchExperience` (same flag as Task 05)
- **Dependencies:** Tasks 01, 05
- **Key Deliverables:**
  - Modern card-based layout
  - Avatar, name, location, key profile info
  - Distance from selected point (when using map search)
  - Responsive design
  - Consistent with JordnaerPalette design system

**Design Considerations:**
- Cards display distance info from map-based search
- Works in split view (map + list) and list-only view
- Clicking card can highlight corresponding map marker

---

### Phase 5: Polish (Independent UI Improvements)

#### Task 07: Account Page Refresh
- **Priority:** Medium - pure UI polish
- **Why Now:** No functional dependencies, can be done anytime after design system
- **Design System Ready:** Uses warmer UI/UX from Task 01
- **Dependencies:** Task 01 (design system)
- **Low Risk:** Isolated pages, won't affect core features
- **Scope:** All 30 Account & Manage pages
- **Key Deliverables:**
  - Consistent spacing and visual hierarchy
  - Responsive design (SSR-compatible)
  - Pure CSS interactions (no JavaScript)
  - Server-side validation only

#### Task 08: Improve Chat UI
- **Priority:** Low - pure polish
- **Why Last:** Completely isolated from other features, no dependencies
- **Design System Ready:** Uses warmer UI/UX from Task 01
- **Dependencies:** Task 01 (design system)
- **Low Risk:** Independent feature
- **Key Deliverables:**
  - Modern chat bubble design
  - Clear sent/received message distinction
  - Smooth scrolling and animations
  - Responsive design

---

## Feature Flag Strategy

### Single Flag for New Search Experience

Both Task 05 (Map Search) and Task 06 (User Search Display) share a single feature flag:

```csharp
// appsettings.json
{
  "FeatureFlags": {
    "EnableNewSearchExperience": false
  }
}
```

### Benefits of Single Flag Approach

1. **Simplicity:** One toggle controls entire new search UX
2. **Consistency:** Map and improved display always deploy together
3. **Easy Rollback:** Single switch to revert to old behavior if issues arise
4. **A/B Testing:** Enable for subset of users to test
5. **Gradual Rollout:** Enable for beta users first, then general availability

### Flag Implementation Checklist

- [ ] Add `EnableNewSearchExperience` to appsettings.json (default: false)
- [ ] Create feature flag configuration service/interface
- [ ] Implement conditional rendering in search pages
- [ ] Test flag = false (existing behavior)
- [ ] Test flag = true (new behavior)
- [ ] Document how to toggle flag

---

## Dependency Graph

```
01: Warmer UI/UX (foundation)
    ├── 03: Finish Posts
    ├── 04: Groups Hub
    ├── 05: Map Search ────┐
    ├── 06: User Search ───┤ (both use same feature flag)
    ├── 07: Account Pages
    └── 08: Chat UI

02: NetTopologySuite (backend)
    └── 05: Map Search

03: Finish Posts
    └── 05: Map Search (posts searchable)

04: Groups Hub
    └── 05: Map Search (groups searchable)

05: Map Search 🚩
    └── 06: User Search (benefits from map integration)

07: Account Pages (independent)

08: Chat UI (independent)
```

---

## Risk Mitigation

### Why Feature Flags Matter

**Tasks 05 and 06** are complex changes to core search functionality. Feature flags allow:

1. **Safe Deployment:** Ship code without activating it
2. **Incremental Testing:** Test in production with limited users
3. **Quick Rollback:** Instantly disable if problems found
4. **Confidence:** No fear of breaking existing functionality

### Rollback Plan

If new search experience has issues:

1. Set `EnableNewSearchExperience = false` in appsettings.json
2. Restart application (or reload config if hot-reload enabled)
3. Users immediately see old search UI
4. Fix issues in new code
5. Re-enable when ready

---

## Summary: Why This Order Works

1. **Task 01 first** → Prevents rework on all UI tasks
2. **Task 02 early** → Backend ready before frontend needs it
3. **Tasks 03-04** → Content ready before map search
4. **Task 05** → Map search has all prerequisites
5. **Task 06** → User display benefits from map integration design
6. **Tasks 07-08** → Polish tasks, no blockers, can be done anytime

**Feature flags on Tasks 05-06** → Safe deployment of complex features

This order minimizes duplicate work, respects dependencies, and reduces risk.
