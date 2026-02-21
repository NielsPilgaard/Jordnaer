# Mine Grupper – Overhaul

## Goal

Replace the current tab-based layout in [MyGroups.razor](../src/web/Jordnaer/Pages/Groups/MyGroups.razor) with a single-page dashboard that shows all four group categories at a glance, without requiring the user to click through tabs.

## Current State

[MyGroups.razor](../src/web/Jordnaer/Pages/Groups/MyGroups.razor) uses `MudTabs` with three tabs:
1. "Grupper Jeg Ejer" – owned groups
2. "Medlem Af" – member groups
3. "Afventende" – pending requests (with a badge showing count)

Each tab renders a `MudGrid` of `GroupSummaryCard` components (xs=12, sm=6, md=4).

[GroupSummaryCard.razor](../src/web/Jordnaer/Features/Groups/GroupSummaryCard.razor) uses `MudCard` with a large icon, a readonly location text field, and group name as `h4` – visually heavy and inconsistent with the discovery flow.

## Required Changes

### 1. Remove MudTabs – Use Sections Instead

Replace the `MudTabs`/`MudTabPanel` structure with four clearly labelled vertical sections rendered sequentially on the page. Each section has:
- A section header (`MudText Typo="Typo.h5"`) with an icon matching the current tab icon
- The group grid (or an empty-state `MudAlert`)

Section order:
1. **Grupper Jeg Ejer** – `Icons.Material.Filled.Star` – groups where `OwnershipLevel` is `Owner` or `InheritsOwnership` and `MembershipStatus` is `Active`
2. **Grupper jeg er admin i med afventende anmodninger** – `Icons.Material.Filled.AdminPanelSettings` – groups where `PermissionLevel` is `Admin` AND `_pendingCounts[group.Group.Id] > 0` (i.e. the user is admin but not owner, and there are pending members). Only show this section if there are any such groups.
3. **Jeg er Medlem Af** – `Icons.Material.Filled.Group` – groups where `MembershipStatus` is `Active` and `OwnershipLevel` is `Member` or `None`
4. **Afventende Anmodninger** – `Icons.Material.Filled.Schedule` – groups where `MembershipStatus` is `PendingApprovalFromGroup` or `PendingApprovalFromUser`

Sections 1, 3, and 4 should always render (with empty state if empty). Section 2 only renders if the list is non-empty.

Remove the top-level `MudAlert` about pending requests (it becomes redundant).

### 2. Replace GroupSummaryCard with GroupCard-style layout

Replace usage of `GroupSummaryCard` in [MyGroups.razor](../src/web/Jordnaer/Pages/Groups/MyGroups.razor) with a new component `MyGroupCard.razor` (create in `src/web/Jordnaer/Features/Groups/`).

`MyGroupCard` should look like [GroupCard.razor](../src/web/Jordnaer/Features/GroupSearch/GroupCard.razor) but with additional management actions overlaid:

- Use the same `MudPaper` + avatar + name + location + member count + description + categories layout as `GroupCard`
- Add a row of action chips/buttons at the bottom of the card (inside the paper, below categories):
  - If `OwnershipLevel` is `Owner` or `PermissionLevel` is `Admin`: show an edit button `MudIconButton` with `Icons.Material.Filled.Edit`, href to `/groups/{GroupId}/edit`
  - If `PendingRequestCount > 0`: show a `MudBadge` wrapping a `MudIconButton` with `Icons.Material.Filled.PersonAdd`, `Color.Warning`, href to `/groups/{GroupName}/members`, title = "{count} afventende anmodning(er)"
  - If `MembershipStatus` is `PendingApprovalFromGroup`: show a `MudChip` with text "Afventer godkendelse" and `Icons.Material.Filled.HourglassTop`
  - If `MembershipStatus` is `PendingApprovalFromUser`: show a `MudChip` with text "Invitation modtaget" and `Icons.Material.Filled.MailOutline`
- Card is wrapped in an `<a>` tag linking to `/groups/{GroupName}` (same as `GroupCard`)
- Apply the same hover CSS as `GroupCard` (translateY(-2px) + elevated box-shadow)

Parameters for `MyGroupCard`:
```csharp
[Parameter] public required UserGroupAccess UserGroupAccess { get; set; }
[Parameter] public int PendingRequestCount { get; set; } = 0;
```

### 3. Keep unchanged

- The `@code` block data loading logic in `MyGroups.razor` stays the same
- The header (back button + "Opret Gruppe" button)
- `MudLoading` wrapper
- `MudContainer MaxWidth="MaxWidth.Large"`
- The `GroupSummaryCard` component file itself (it may be used elsewhere)

## File Changes

| File | Action |
|------|--------|
| `src/web/Jordnaer/Pages/Groups/MyGroups.razor` | Modify – replace tabs with sections, replace `GroupSummaryCard` with `MyGroupCard` |
| `src/web/Jordnaer/Features/Groups/MyGroupCard.razor` | Create – new card component |

## Verification

1. Build succeeds: `dotnet build`
2. Navigate to `/groups/my-groups` and confirm:
   - All four sections are visible without clicking tabs
   - Each section shows the correct groups or an empty-state alert
   - Cards look like the discovery `GroupCard` (avatar, compact info row, description, categories)
   - Edit and pending-member buttons appear on owned/admin group cards
   - Pending status chips appear on pending-request cards
   - Clicking a card navigates to the group page
