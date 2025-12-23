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
- Edit-on-row-click is too error-prone (accidental edits)
- Inline editing clutters the table with dropdowns
- No clear visual distinction between view mode and edit mode
- Action buttons column is visually cluttered
- Unclear which rows are editable vs read-only
- No bulk operations support

## Design Decision: Tables vs Cards

**Tables are the correct choice for this use case** because:
- Admins need to compare permissions across multiple users (tables excel at comparison tasks)
- Sorting and filtering by columns is natural for permission management
- Bulk operations are important for admin workflows
- Scannability: Tabular alignment makes it easy to scan structured data
- Cards would only work for rich visual content or individual deep consideration

**Verdict: Keep using MudDataGrid, but fix the interaction pattern.**

## Requirements

### 1. Read-Only Display with Explicit Actions

**Key change: Set `ReadOnly="true"` on MudDataGrid**

- Remove `EditMode` and `EditTrigger` attributes completely
- Display all data as read-only text/chips
- Add explicit "Actions" column with menu/buttons
- Use `MudChip` with colors for status/roles (better visual scannability)
- Keep pending approvals sorted to top (existing behavior)

**Visual improvements:**
- **Ownership Level:** Display as colored chip (Primary color for Owner, Secondary for others)
- **Permission Level:** Display as colored chip (Success for Admin, Info for Write)
- **Membership Status:** Display as colored chip:
  - Pending Approval: Warning (yellow)
  - Active: Success (green)
  - Rejected: Default (gray)
  - Pending from User: Info (blue)

### 2. Action Column with Kebab Menu

**Replace inline buttons with organized menu:**

```razor
<TemplateColumn Title="Actions" Sortable="false">
    <CellTemplate>
        @if (CanEditUser(context.Item))
        {
            <MudMenu Icon="@Icons.Material.Filled.MoreVert" Size="Size.Small" Dense="true">
                <MudMenuItem Icon="@Icons.Material.Filled.Edit"
                             OnClick="@(() => OpenEditDialog(context.Item))">
                    Edit Permissions
                </MudMenuItem>

                @if (context.Item.MembershipStatus == MembershipStatus.PendingApprovalFromGroup)
                {
                    <MudMenuItem Icon="@Icons.Material.Filled.Check"
                                 OnClick="@(() => ApproveMember(context.Item))">
                        Approve
                    </MudMenuItem>
                    <MudMenuItem Icon="@Icons.Material.Filled.Close"
                                 OnClick="@(() => RejectMember(context.Item))">
                        Reject
                    </MudMenuItem>
                }
                else if (context.Item.MembershipStatus == MembershipStatus.Active)
                {
                    <MudDivider />
                    <MudMenuItem Icon="@Icons.Material.Filled.PersonRemove"
                                 OnClick="@(() => RemoveMember(context.Item))">
                        Remove Member
                    </MudMenuItem>
                }
            </MudMenu>
        }
    </CellTemplate>
</TemplateColumn>
```

**Benefits:**
- Less visual clutter (one menu icon vs multiple buttons)
- Actions are contextual to status
- Icons + text make actions clear
- Can't accidentally click (menu must be opened first)

### 3. Modal Dialog for Permission Editing

**Replace cell editing with a proper dialog:**

```razor
<MudDialog @bind-Visible="_editDialogVisible" Options="_dialogOptions">
    <TitleContent>
        <MudText Typo="Typo.h6">
            <MudIcon Icon="@Icons.Material.Filled.Edit" Class="mr-2" />
            Edit Permissions for @_editingMember?.UserDisplayName
        </MudText>
    </TitleContent>
    <DialogContent>
        <MudStack Spacing="3">
            <MudSelect @bind-Value="_editingMember.OwnershipLevel"
                       Label="Ownership Level"
                       HelperText="Determines ownership rights in the group">
                @foreach (var level in Enum.GetValues<OwnershipLevel>())
                {
                    <MudSelectItem Value="level">
                        @level.ToDisplayName()
                    </MudSelectItem>
                }
            </MudSelect>

            <MudSelect @bind-Value="_editingMember.PermissionLevel"
                       Label="Permission Level"
                       HelperText="Controls what actions the member can perform">
                @foreach (var level in Enum.GetValues<PermissionLevel>())
                {
                    <MudSelectItem Value="level">
                        @level.ToDisplayName()
                    </MudSelectItem>
                }
            </MudSelect>

            <MudAlert Severity="Severity.Info" Dense="true">
                Changes take effect immediately after saving.
            </MudAlert>
        </MudStack>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="CancelEdit">Cancel</MudButton>
        <MudButton Color="Color.Primary"
                   Variant="Variant.Filled"
                   OnClick="SaveEdit">
            Save Changes
        </MudButton>
    </DialogActions>
</MudDialog>
```

**Benefits:**
- More space for dropdowns and explanations
- Explicit save/cancel actions
- Can add helper text and warnings
- Better mobile experience
- Prevents accidental edits

### 4. Toolbar with Search and Bulk Actions

**Add toolbar above the table:**

```razor
<ToolBarContent>
    <MudText Typo="Typo.h6">Members (@_memberships.Count)</MudText>

    @if (_pendingCount > 0)
    {
        <MudBadge Content="_pendingCount" Color="Color.Warning" Overlap="true" Class="ml-2">
            <MudIcon Icon="@Icons.Material.Filled.PersonAdd" />
        </MudBadge>
    }

    <MudSpacer />

    <MudTextField @bind-Value="_searchString"
                  Placeholder="Search members..."
                  Adornment="Adornment.Start"
                  AdornmentIcon="@Icons.Material.Filled.Search"
                  IconSize="Size.Small"
                  Margin="Margin.Dense" />

    @if (_selectedItems?.Count > 0)
    {
        <MudButton Color="Color.Primary"
                   StartIcon="@Icons.Material.Filled.Edit"
                   OnClick="OpenBulkEditDialog">
            Bulk Edit (@_selectedItems.Count)
        </MudButton>
    }
</ToolBarContent>
```

**Features:**
- Member count display
- Badge for pending approvals (visual indicator)
- Quick search (filters as you type)
- Bulk edit button (appears when items selected)

### 5. Multi-Select for Bulk Operations

**Enable checkbox selection:**

```razor
<MudDataGrid T="GroupMembershipDto"
             Items="_memberships"
             ReadOnly="true"
             MultiSelection="true"
             @bind-SelectedItems="_selectedItems"
             QuickFilter="_quickFilterFunc">

    <Columns>
        <SelectColumn T="GroupMembershipDto" />
        <!-- Other columns -->
    </Columns>
</MudDataGrid>
```

**Bulk operations:**
- Change permission level for multiple users at once
- Remove multiple members
- Approve multiple pending requests

### 6. Quick Filter Implementation

**Client-side filtering for instant results:**

```csharp
private string _searchString = "";

private Func<GroupMembershipDto, bool> _quickFilterFunc => x =>
{
    if (string.IsNullOrWhiteSpace(_searchString))
        return true;

    if (x.UserDisplayName.Contains(_searchString, StringComparison.OrdinalIgnoreCase))
        return true;

    return false;
};
```

### 7. Confirmation for Destructive Actions

**Add confirmation dialog for removing members:**

```csharp
private async Task RemoveMember(GroupMembershipDto member)
{
    bool? result = await DialogService.ShowMessageBox(
        "Remove Member",
        $"Are you sure you want to remove {member.UserDisplayName} from the group?",
        yesText: "Remove",
        cancelText: "Cancel");

    if (result == true)
    {
        await ChangeMembershipStatusAsync(member, MembershipStatus.Rejected);
    }
}
```

## Acceptance Criteria

### Layout & Design
- [ ] MudDataGrid set to ReadOnly="true"
- [ ] EditMode and EditTrigger removed completely
- [ ] All data displayed as read-only (chips for status/roles)
- [ ] Clean, scannable table layout
- [ ] Fully responsive (mobile, tablet, desktop)
- [ ] Visual hierarchy clear and scannable

### Status Display
- [ ] Ownership Level displayed as colored MudChip
- [ ] Permission Level displayed as colored MudChip
- [ ] Membership Status displayed as colored MudChip with appropriate colors:
  - [ ] Pending Approval: Warning (yellow)
  - [ ] Active: Success (green)
  - [ ] Rejected: Default (gray)
  - [ ] Pending from User: Info (blue)

### Actions Column
- [ ] Kebab menu (three dots) for each row
- [ ] Menu items are contextual based on membership status
- [ ] Icons + text labels for all menu items
- [ ] Cannot edit own membership
- [ ] Cannot edit/remove owner (menu disabled for these rows)
- [ ] Confirmation dialog for destructive actions (Remove Member)

### Edit Dialog
- [ ] Modal dialog opens when "Edit Permissions" clicked
- [ ] Dialog shows current ownership and permission levels
- [ ] MudSelect dropdowns for both levels
- [ ] Helper text explains each field
- [ ] Clear Save/Cancel buttons
- [ ] Changes saved via existing SaveChangesAsync method
- [ ] Dialog closes on save/cancel
- [ ] Optimistic UI updates with rollback on error

### Toolbar
- [ ] Member count displayed
- [ ] Pending approval badge shown if count > 0
- [ ] Search field filters table instantly (client-side)
- [ ] Bulk edit button appears when items selected
- [ ] Toolbar is responsive

### Multi-Select & Bulk Operations
- [ ] Checkbox column added (SelectColumn)
- [ ] MultiSelection="true" enabled
- [ ] Bulk edit dialog for changing permissions on multiple users
- [ ] Bulk approve for multiple pending requests
- [ ] Bulk remove for multiple members
- [ ] Selected count shown in toolbar

### Quick Filter
- [ ] Search filters by user display name
- [ ] Case-insensitive filtering
- [ ] Instant results (no delay)
- [ ] Works across all membership statuses

### Functionality
- [ ] All existing functionality preserved
- [ ] Optimistic updates on changes
- [ ] Error handling with rollback
- [ ] Success/error snackbar messages
- [ ] Sorting by status maintained (pending first)
- [ ] No accidental edits possible (explicit actions required)

## Files to Modify

**Modify:**
- [Members.razor](src/web/Jordnaer/Pages/Groups/Members.razor) - Major refactor

**Key changes to Members.razor:**
1. Change `ReadOnly="false"` → `ReadOnly="true"`
2. Remove `EditMode` and `EditTrigger` attributes
3. Replace `<PropertyColumn>` edit templates with `<CellTemplate>` showing chips
4. Add `<TemplateColumn>` for Actions with MudMenu
5. Add `<ToolBarContent>` with search and bulk actions
6. Add `<SelectColumn>` for multi-select
7. Add `MultiSelection="true"` and `QuickFilter` attributes
8. Add modal dialog for editing permissions
9. Add confirmation dialog for destructive actions
10. Update `UpdateOwnershipLevel` and `UpdatePermissionLevel` methods to work with dialog

**May create (optional):**
- `src/web/Jordnaer/Pages/Groups/Components/EditMemberDialog.razor` - Reusable edit dialog component
- `src/web/Jordnaer/Pages/Groups/Components/BulkEditDialog.razor` - Bulk edit dialog

## MudBlazor Components Used

- `MudDataGrid` - Table with filtering, sorting, multi-select
- `MudChip` - Status/role badges with colors
- `MudMenu` + `MudMenuItem` - Kebab menu for actions
- `MudDialog` - Edit and confirmation dialogs
- `MudTextField` - Search input in toolbar
- `MudButton` - Action buttons
- `MudBadge` - Pending count indicator
- `MudSelect` - Dropdowns in edit dialog
- `MudAlert` - Info messages in dialog
- `SelectColumn` - Checkbox column for multi-select

## Technical Notes

- Preserve existing business logic (validation, permissions checks)
- Keep the `SortOrder` dictionary for status sorting
- Maintain GDPR compliance (no changes needed)
- Client-side filtering for search (QuickFilter)
- Keep existing `SaveChangesAsync`, `UpdateOwnershipLevel`, `UpdatePermissionLevel` methods
- Reuse existing `CanEditUser` logic (prevent editing self/owner)
- Use existing `ChangeMembershipStatusAsync` for approve/reject/remove actions
- Add `IDialogService` injection for confirmation dialogs
- Consider virtualization if member lists exceed 100 users

## User Experience Goals

- **Scannability:** Clean read-only table is easy to scan without visual clutter
- **Safety:** Explicit actions prevent accidental edits, confirmation for destructive actions
- **Efficiency:** Bulk operations for managing many users at once, instant search
- **Clarity:** Color-coded chips make status immediately obvious
- **Discoverability:** All actions organized in contextual kebab menu
- **Responsiveness:** Works well on all screen sizes, modals are mobile-friendly
- **Performance:** Smooth interactions, no lag on member lists up to 100+ members

## Why This Approach?

**Tables are optimal for admin user management because:**
1. Comparison: Admins need to scan across users to compare permissions
2. Bulk operations: Multi-select and batch actions are natural with tables
3. Sorting/filtering: Column-based operations align with mental model
4. Scannability: Tabular data is faster to process than cards for structured data

**Modal dialogs are better than inline editing because:**
1. More space for dropdowns and helper text
2. Explicit save/cancel prevents accidental changes
3. Can show warnings and validation messages
4. Better mobile experience (full-screen on small screens)
5. Clearer edit state (you're "in" edit mode vs "is this row editable?")

**Read-only display with action column is superior because:**
1. Visual clarity: No clutter from edit controls
2. Prevents accidents: Edit-on-click was too error-prone
3. Consistent state: Always know what you're looking at
4. Progressive disclosure: Actions hidden until needed (in menu)
