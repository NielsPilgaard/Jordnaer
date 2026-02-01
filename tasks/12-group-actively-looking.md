# Task: Add "Actively Looking for Members" Flag to Groups

## Overview
Groups need to be able to indicate whether they are actively searching for new members. This involves database changes, search functionality, and UI updates across group create/edit/details pages.

## Files to Modify

### 1. Database/Models

**`src/shared/Jordnaer.Shared/Database/Group.cs`**
- Add new property: `public bool IsActiveLookingForMembers { get; set; } = false;`
- Place it after the `CreatedUtc` property
- Default to `false` (groups are not actively looking by default)

**`src/shared/Jordnaer.Shared/Groups/GroupSlim.cs`**
- Add matching property: `public bool IsActiveLookingForMembers { get; set; }`
- This DTO is used for search results and API responses

### 2. Database Migration
- Run: `dotnet ef migrations add Add_IsActiveLookingForMembers_To_Groups --project src/web/Jordnaer`
- The migration will add the boolean column with default value `false`

### 3. Search Functionality

**`src/shared/Jordnaer.Shared/Groups/GroupSearchFilter.cs`**
- Add optional filter property: `public bool? IsActiveLookingForMembers { get; set; }`
- This allows users to filter to only show groups actively looking for members

**`src/web/Jordnaer/Features/GroupSearch/QueryableGroupExtensions.cs`**
- Add new extension method:
```csharp
internal static IQueryable<Group> ApplyActiveLookingFilter(
    this IQueryable<Group> groups,
    bool? isActiveLooking)
{
    if (isActiveLooking.HasValue && isActiveLooking.Value)
    {
        groups = groups.Where(group => group.IsActiveLookingForMembers);
    }
    return groups;
}
```

**`src/web/Jordnaer/Features/GroupSearch/GroupSearchService.cs`**
- Add `.ApplyActiveLookingFilter(filter.IsActiveLookingForMembers)` to the query chain in `GetGroupsAsync`
- Update the `ToGroupSlim` mapping to include `IsActiveLookingForMembers`

**`src/web/Jordnaer/Features/GroupSearch/GroupSearchForm.razor`**
- Add a checkbox filter: "Kun grupper der søger medlemmer" (Only groups looking for members)
- Wire it to the filter and trigger search on change

### 4. Group Create/Edit Forms

**`src/web/Jordnaer/Pages/Groups/CreateGroup.razor`**
- Add checkbox in the form (after categories section):
```razor
<MudItem xs="12">
    <MudCheckBox @bind-Value="_group.IsActiveLookingForMembers"
                 Label="Vi søger aktivt efter nye medlemmer"
                 Color="Color.Primary" />
</MudItem>
```

**`src/web/Jordnaer/Pages/Groups/EditGroup.razor`**
- Add the same checkbox as in CreateGroup
- The value will be loaded from the existing group data

### 5. Group Service

**`src/web/Jordnaer/Features/Groups/GroupService.cs`**
- In the `UpdateExistingGroupAsync` method, add:
```csharp
currentGroup.IsActiveLookingForMembers = updatedGroup.IsActiveLookingForMembers;
```

### 6. UI Display Components

**`src/web/Jordnaer/Features/GroupSearch/GroupCard.razor`**
- Add visual indicator when group is actively looking:
```razor
@if (Group.IsActiveLookingForMembers)
{
    <MudChip T="string" Variant="Variant.Filled" Color="Color.Success" Size="Size.Small">
        Søger medlemmer
    </MudChip>
}
```
- Place this chip near the categories or member count

**`src/web/Jordnaer/Pages/Groups/GroupDetails.razor`**
- Add the same visual indicator in the group details header/card area
- Consider making it more prominent on the details page

## Implementation Notes

### Existing Patterns to Follow
- Boolean flags with defaults: See `Partner.cs` which uses `public bool CanHaveAd { get; set; } = true;`
- Filter extensions: Follow the pattern in `QueryableGroupExtensions.cs` with `ApplyNameFilter` and `ApplyCategoryFilter`
- UI components: Use MudBlazor's `MudCheckBox` and `MudChip` components (already used throughout the codebase)

### Danish Translations
- "Vi søger aktivt efter nye medlemmer" = "We are actively looking for new members"
- "Søger medlemmer" = "Looking for members"
- "Kun grupper der søger medlemmer" = "Only groups looking for members"

### Testing Considerations
1. Create a new group with the flag enabled - verify it persists
2. Edit an existing group to toggle the flag - verify update works
3. Search with the filter enabled - verify only matching groups appear
4. Verify the visual indicator displays correctly on GroupCard and GroupDetails

## File Reference Summary

| File | Change Type |
|------|-------------|
| `src/shared/Jordnaer.Shared/Database/Group.cs` | Add property |
| `src/shared/Jordnaer.Shared/Groups/GroupSlim.cs` | Add property |
| `src/shared/Jordnaer.Shared/Groups/GroupSearchFilter.cs` | Add filter property |
| `src/web/Jordnaer/Features/GroupSearch/QueryableGroupExtensions.cs` | Add filter method |
| `src/web/Jordnaer/Features/GroupSearch/GroupSearchService.cs` | Apply filter + update mapping |
| `src/web/Jordnaer/Features/GroupSearch/GroupSearchForm.razor` | Add checkbox filter |
| `src/web/Jordnaer/Features/GroupSearch/GroupCard.razor` | Add visual indicator |
| `src/web/Jordnaer/Pages/Groups/CreateGroup.razor` | Add checkbox |
| `src/web/Jordnaer/Pages/Groups/EditGroup.razor` | Add checkbox |
| `src/web/Jordnaer/Pages/Groups/GroupDetails.razor` | Add visual indicator |
| `src/web/Jordnaer/Features/Groups/GroupService.cs` | Update mapping in UpdateExistingGroupAsync |
