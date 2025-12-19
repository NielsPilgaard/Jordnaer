# Task 04: Navigation & Authenticated Experience

## Context
**App:** Jordnaer (.NET Blazor Server)  
**Area:** Layout (`TopBar.razor`), Home (`MainLandingPage.razor`), and New Authenticated Dashboard  
**Priority:** High  
**Design Reference:** See `tasks/03-warmer-ui-ux.md` for design system guidelines

## Objective
Improve navigation discoverability by revamping the Topbar and creating a dedicated dashboard for authenticated users. Users should immediately see where they can navigate (Home, Chat, Groups, Users, Posts, Profile) regardless of their device.

## Current State
- **Topbar:** Uses only icons, which can be ambiguous. It feels a bit "bare" and lacks labels.
- **Mobile Nav:** No dedicated mobile-optimized menu (just a row of icons).
- **Home Page:** Logged-in users see the same landing page as guests, which isn't personalized or helpful for frequent users.
- **First Login:** `FirstLogin.razor` exists but is intended only for the very first visit and isn't "premium" or reliable enough for regular use.

## Requirements

### 1. Enhanced Topbar
**Goal:** Make navigation options explicit and accessible.

- **Desktop:**
  - Add text labels next to icons (e.g., "Søg", "Grupper", "Chat", "Opslag", "Profil").
  - Use Cherry Bomb One or Open Sans Semi-Bold for labels.
  - Maintain a warm, gummy aesthetic with subtle hover effects.
  - Ensure clear active states for the current page.
- **Mobile:**
  - Implement a dropdown or "hamburger" menu to better show navigation options.
  - Ensure tap targets are large (min 44px).
  - Consider a bottom navigation bar or a side drawer if it fits the "warm" aesthetic better.
- **SSR Considerations:** 
  - The menu should be functional/visible even before full Blazor hydration if possible.
  - Use standard HTML/CSS for the layout of the topbar to ensure it renders instantly.

### 2. Authenticated Dashboard (`UserDashboard.razor`)
**Goal:** Create a high-quality landing page for logged-in users.

- Modify `MainLandingPage.razor` to show a `UserDashboard` component for authorized users.
- **Features to Showcase:**
  - **Søg efter familier**: Find other families near you.
  - **Dine Grupper**: Quick access to joined groups or discovering new ones.
  - **Mine Beskeder**: Direct link to chat.
  - **Seneste Opslag**: Link to the feed/posts.
  - **Min Profil**: Edit profile and children.
- **Design:**
  - Use the "Card" pattern from `FirstLogin.razor` but elevate it with better spacing, icons, and brand colors (GLÆDE, RO, OMSORG, LEG).
  - Add a "Welcome back, [Name]" header.
  - Ensure the cards look premium (soft shadows, rounded corners, warm colors).

### 3. Visual Polish & Animations
- Add subtle micro-animations (e.g., hover scaling on cards, smooth transition for the mobile menu).
- Use `MiniDivider` to separate sections if the dashboard has multiple parts.
- Ensure the color palette follows the "Warmer UI/UX" guide.

## Acceptance Criteria

### Topbar
- [ ] Topbar links have both icons and text labels on desktop.
- [ ] A functional mobile menu (dropdown or drawer) is implemented.
- [ ] Active page is clearly highlighted in the navigation.
- [ ] Design matches the warm/friendly brand aesthetic.
- [ ] Topbar remains fixed or behaves predictably during scroll.

### User Dashboard
- [ ] Logged-in users are automatically shown the dashboard at `/`.
- [ ] Dashboard contains clear links to all core features: Users, Groups, Chat, Posts, and Profile.
- [ ] Dashboard uses the design system's typography (Cherry Bomb One for headers).
- [ ] Layout is responsive (cards stack on mobile, grid on desktop).
- [ ] Loading state is handled gracefully.

### Technical & Performance
- [ ] Initial page load (SSR) is < 200ms.
- [ ] No layout shift (CLS) during navigation menu opening/closing.
- [ ] Minimal use of heavy Blazor components for the topbar to ensure SSR reliability.
- [ ] Images/Icons are optimized.

## Implementation Steps

### Phase 1: Topbar Refactor
1. Update `TopBar.razor` to include text labels for desktop view.
2. Implement a responsive mobile menu (CSS-based toggle or simple JS to ensure SSR speed).
3. Apply brand colors and typography.

### Phase 2: User Dashboard Component
1. Create `src/web/Jordnaer/Features/Dashboard/UserDashboard.razor`.
2. Design the "Quick Actions" cards based on the `FeatureCard` pattern but optimized for regular users.
3. Integrate user-specific data (e.g., "Welcome [Name]").

### Phase 3: Home Page Logic & FirstLogin Removal
1. Update `MainLandingPage.razor` to use `<AuthorizeView>` to switch between Guest Landing Page and User Dashboard.
2. **Remove `FirstLogin.razor` entirely.**
3. Update `Login.razor` and `ExternalLogin.razor` to redirect to `/` (Home) instead of `/first-login`.
4. Add `?FirstLogin=true` to these redirects so `MainLayout.razor` can display the welcome snackbar.

### Phase 4: Polish
1. Add micro-animations.
2. Test responsiveness across all breakpoints.
3. Verify SSR performance.

## Files to Modify
- `src/web/Jordnaer/Pages/Shared/TopBar.razor`
- `src/web/Jordnaer/Pages/Home/MainLandingPage.razor`
- `src/web/Jordnaer/Features/Dashboard/UserDashboard.razor` (New)
- `src/web/Jordnaer/Components/Account/Pages/Login.razor`
- `src/web/Jordnaer/Components/Account/Pages/ExternalLogin.razor`
- `src/web/Jordnaer/wwwroot/css/app.css` or `landing-page.css`

## Files to Delete
- `src/web/Jordnaer/Pages/Registration/FirstLogin.razor`


## Success Metrics
- Users can navigate to any major feature in 1 click from the dashboard.
- Mobile users report improved ease of use with the new menu.
- 100% compliance with the "Warmer UI/UX" design system.

