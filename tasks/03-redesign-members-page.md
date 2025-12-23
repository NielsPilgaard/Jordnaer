# Task 03: Redesign Group Members Management Page

## Context

**App:** Jordnaer (.NET Blazor Server)
**Area:** Group Management UI
**Priority:** Medium
**Related:** Task 02 (Backoffice Claims Management) - will use similar patterns after this redesign

## Objective

Redesign the group members management page to improve UX, visual design, and maintainability. The current implementation uses a clunky DataGrid with edit-on-click that doesn't look good or feel intuitive.

## Current State

- Page location: [Members.razor](src/web/Jordnaer/Pages/Groups/Members.razor)
- Route: `/groups/{GroupName}/members`
- Uses MudDataGrid with `EditMode="Cell"` and `EditTrigger="OnRowClick"`
- Displays: Name, OwnershipLevel, PermissionLevel, MembershipStatus, Actions
- Edit functionality uses inline MudSelect dropdowns in grid cells
- Action buttons change based on membership status (Approve/Reject/Remove/Add)
- Sorts pending approvals to top using custom sort order

**Problems with current design:**
- DataGrid edit-on-click is not intuitive
- Inline editing in table cells feels cramped
- Select dropdowns in cells look awkward
- Action buttons column is visually cluttered
- Not mobile-friendly (table layout)
- Unclear which rows are editable vs read-only
- Mixing display and edit modes in same view is confusing

## Requirements

### 1. Card-Based Layout

Replace the DataGrid with a card-based design:
- Each member displayed as a MudCard
- Grouped by membership status (Pending Approvals, Active Members, Rejected)
- Use MudExpansionPanels for each group (collapsible sections)
- Mobile-first responsive design
- Avatar display with user profile picture
- Link to user profile from card

**Card structure:**
```razor
<MudCard Class="mb-3">
    <MudCardContent>
        <MudStack Row AlignItems="AlignItems.Center">
            <!-- Avatar + Name -->
            <MudAvatar Size="Size.Large">
                <MudImage Src="@member.ProfilePictureUrl" />
            </MudAvatar>

            <MudStack Spacing="1" Style="flex: 1">
                <MudText Typo="Typo.h6">@member.UserDisplayName</MudText>

                <!-- Status badges -->
                <MudStack Row Spacing="1">
                    <MudChip Size="Size.Small" Color="GetStatusColor()">
                        @member.MembershipStatus.ToDisplayName()
                    </MudChip>
                    <MudChip Size="Size.Small" Variant="Variant.Outlined">
                        @member.PermissionLevel.ToDisplayName()
                    </MudChip>
                    @if (member.OwnershipLevel != OwnershipLevel.None)
                    {
                        <MudChip Size="Size.Small" Color="Color.Primary">
                            @member.OwnershipLevel.ToDisplayName()
                        </MudChip>
                    }
                </MudStack>
            </MudStack>

            <!-- Actions Menu -->
            <MudMenu Icon="@Icons.Material.Filled.MoreVert">
                <!-- Context menu items -->
            </MudMenu>
        </MudStack>
    </MudCardContent>
</MudCard>
```

### 2. Improved Status Display

**Visual status indicators:**
- **Pending Approval from Group:** Yellow/Warning badge, show "Approve" and "Reject" buttons prominently
- **Active:** Green/Success badge, show "Edit Permissions" and "Remove Member" in menu
- **Rejected:** Gray badge, show "Add Member" button
- **Pending Approval from User:** Blue/Info badge, no actions available

**Count badges:**
- Show count of pending approvals in expansion panel header
- Example: "Pending Approvals (3)"
- Highlight if there are pending items

### 3. Action Menu System

Replace inline action buttons with MudMenu:
- Three-dot menu icon on each card
- Menu items based on membership status and user permissions
- Icon + text for each action (clearer than buttons)
- Confirmation dialogs for destructive actions (Remove Member)

**Menu items by status:**
- **Active Member:**
  - Edit Permissions → Opens dialog
  - Change Ownership Level → Opens dialog
  - Remove Member → Confirmation dialog
- **Pending Approval:**
  - Approve Request → Immediate action
  - Reject Request → Immediate action
- **Rejected:**
  - Add as Member → Immediate action

### 4. Edit Dialogs

Replace inline editing with modal dialogs:
- **Edit Permissions Dialog:**
  - Shows current permission level
  - Radio buttons or select for new level
  - Save/Cancel buttons
  - Explanation of each permission level
- **Edit Ownership Dialog:**
  - Shows current ownership level
  - Radio buttons or select for new level
  - Save/Cancel buttons
  - Warning if changing owner

**Benefits:**
- More space for explanations
- Clearer what's being changed
- Can show impact of changes
- Better mobile experience

### 5. Pending Approvals Priority

**Highlight pending approvals:**
- Separate section at top (expanded by default)
- Visual indicator (color, icon, or badge)
- Quick approve/reject actions (no menu needed)
- Show timestamp of request if available
- Empty state message if no pending approvals

### 6. Search and Filter

Add search/filter bar:
- Search by member name (instant filter)
- Filter by status (dropdown)
- Filter by permission level (dropdown)
- Clear filters button
- Show result count

### 7. Empty States

**Graceful empty states for each section:**
- No pending approvals: "No pending membership requests"
- No active members: "No active members yet"
- No rejected members: "No rejected members" (can hide this section)
- Search returns no results: "No members match your search"

## Acceptance Criteria

### Layout & Design
- [ ] Card-based layout replaces DataGrid
- [ ] Grouped by membership status with expansion panels
- [ ] Fully responsive (mobile, tablet, desktop)
- [ ] Avatar + name prominently displayed
- [ ] Profile picture links to user profile
- [ ] Visual hierarchy clear and scannable

### Status Display
- [ ] Status badges use appropriate colors (success, warning, info, error)
- [ ] Permission and ownership displayed as chips
- [ ] Pending approval count shown in section header
- [ ] Each section has appropriate icon

### Actions
- [ ] Three-dot menu on each card
- [ ] Menu items change based on status and permissions
- [ ] Cannot edit own membership
- [ ] Cannot edit/remove owner
- [ ] Confirmation dialog for destructive actions

### Dialogs
- [ ] Edit permissions dialog implemented
- [ ] Edit ownership dialog implemented
- [ ] Dialogs show current values
- [ ] Clear Save/Cancel actions
- [ ] Validation prevents invalid changes

### Pending Approvals
- [ ] Pending section at top, expanded by default
- [ ] Quick approve/reject buttons (no menu)
- [ ] Visual highlighting for pending items
- [ ] Empty state when no pending approvals

### Search & Filter
- [ ] Search by name (instant client-side filter)
- [ ] Filter by status dropdown
- [ ] Filter by permission dropdown
- [ ] Clear filters button
- [ ] Result count displayed

### Empty States
- [ ] Each section has appropriate empty state
- [ ] Search no-results state
- [ ] Empty states are helpful and clear

### Functionality
- [ ] All existing functionality preserved
- [ ] Optimistic updates on changes
- [ ] Error handling with rollback
- [ ] Success/error snackbar messages
- [ ] Sorting by status maintained (pending first)

## Files to Modify

**Modify:**
- [Members.razor](src/web/Jordnaer/Pages/Groups/Members.razor) - Complete redesign

**May create (optional):**
- `src/web/Jordnaer/Pages/Groups/Components/MemberCard.razor` - Reusable member card component
- `src/web/Jordnaer/Pages/Groups/Components/EditPermissionsDialog.razor` - Edit dialog
- `src/web/Jordnaer/Pages/Groups/Components/EditOwnershipDialog.razor` - Edit dialog

## Design References

**Existing good patterns in codebase:**
- [DashboardCard.razor](src/web/Jordnaer/Features/Dashboard/DashboardCard.razor) - Card layout
- [GroupCard.razor](src/web/Jordnaer/Features/Groups/GroupCard.razor) - Group cards (if exists)
- [UserDashboard.razor](src/web/Jordnaer/Features/Dashboard/UserDashboard.razor) - Responsive grid

**MudBlazor components to use:**
- `MudCard` - Card container
- `MudExpansionPanels` / `MudExpansionPanel` - Collapsible sections
- `MudAvatar` + `MudImage` - Profile pictures
- `MudChip` - Status badges
- `MudMenu` + `MudMenuItem` - Action menu
- `MudDialog` - Edit dialogs
- `MudTextField` - Search input
- `MudSelect` - Filter dropdowns

## Technical Notes

- Preserve existing business logic (validation, permissions checks)
- Keep the SortOrder dictionary for status sorting
- Maintain GDPR compliance (no changes needed)
- Client-side filtering for search (avoid unnecessary API calls)
- Consider virtualization if member lists get very long (MudVirtualize)
- Animations: Smooth transitions between states (MudBlazor defaults)

## User Experience Goals

- **Clarity:** Immediately obvious who is pending, active, or rejected
- **Efficiency:** Admins can quickly approve/reject pending members
- **Discoverability:** All actions available through intuitive menu
- **Safety:** Destructive actions require confirmation
- **Responsiveness:** Works well on all screen sizes
- **Performance:** Smooth interactions, no lag on member lists up to 100+ members
