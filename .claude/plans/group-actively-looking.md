# Task: Add "Actively Looking" Flag to Groups and Users

## Overview

Groups and users need to be able to indicate whether they are actively searching for new members/connections. This involves database changes, search functionality, and UI updates across group and user create/edit/details/search pages.

The flag defaults to `true` for both groups and users — new profiles and groups are actively looking by default.

---

## Part A: Groups

### 1. Database/Models

**`src/shared/Jordnaer.Shared/Database/Group.cs`**
- Add new property: `public bool IsActiveLookingForMembers { get; set; } = true;`
- Place it after the `CreatedUtc` property
- Default to `true`

**`src/shared/Jordnaer.Shared/Groups/GroupSlim.cs`**
- Add matching property: `public bool IsActiveLookingForMembers { get; set; }`
- This DTO is used for search results and API responses

### 2. Database Migration

- Run migration script: `./src/web/Jordnaer/add-migration.ps1 Add_IsActiveLooking_To_Groups_And_Users`
- The migration will add the boolean column with default value `true` to both `Groups` and `UserProfiles` tables
- **Note**: Run a single migration covering both Group and UserProfile changes

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
    if (isActiveLooking is true)
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
<MudCheckBox @bind-Value="_group.IsActiveLookingForMembers"
             Label="Vi søger aktivt efter nye medlemmer"
             Color="Color.Primary" />
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

---

## Part B: Users

### 1. Database/Models

**`src/shared/Jordnaer.Shared/Database/UserProfile.cs`**
- Add new property: `public bool IsActiveLooking { get; set; } = true;`
- Place it after the `CreatedUtc` property
- Default to `true`

**`src/shared/Jordnaer.Shared/UserSearch/UserDto.cs`**
- Add matching property: `public bool IsActiveLooking { get; set; }`
- This DTO is used for user search results

**`src/shared/Jordnaer.Shared/Profile/DTO/ProfileDto.cs`**
- Add matching property: `public bool IsActiveLooking { get; set; }`
- This DTO is used for the public profile page

### 2. Database Migration

- Handled together with the Group migration in Part A, step 2

### 3. Search Functionality

**`src/shared/Jordnaer.Shared/UserSearch/UserSearchFilter.cs`**
- Add optional filter property: `public bool? IsActiveLooking { get; set; }`
- Update `GetHashCode()` and `Equals()` to include the new property (required for search result caching)

**`src/web/Jordnaer/Features/UserSearch/QueryableUserProfileExtensions.cs`**
- Add new extension method:
```csharp
internal static IQueryable<UserProfile> ApplyActiveLookingFilter(
    this IQueryable<UserProfile> users,
    bool? isActiveLooking)
{
    if (isActiveLooking is true)
    {
        users = users.Where(user => user.IsActiveLooking);
    }
    return users;
}
```

**`src/web/Jordnaer/Features/UserSearch/UserSearchService.cs`**
- Add `.ApplyActiveLookingFilter(filter.IsActiveLooking)` to the query chain in `GetUsersAsync`
- Update the `UserDto` mapping to include `IsActiveLooking`

**`src/web/Jordnaer/Features/UserSearch/UserSearchForm.razor`**
- Add a checkbox filter: "Kun brugere der aktivt søger" (Only users actively looking)
- Wire it to the filter and trigger search on change
- Place in or near the advanced search section

### 4. User Profile Edit

**`src/web/Jordnaer/Pages/Profile/MyProfile.razor`**
- Add checkbox in the profile edit form (e.g. after the interests/description section):
```razor
<MudCheckBox @bind-Value="_profile.IsActiveLooking"
             Label="Jeg søger aktivt efter nye kontakter"
             Color="Color.Primary" />
```

**Profile Service (update mapping)**
- Ensure the `IsActiveLooking` property is mapped when saving the profile
- Check `IProfileService` implementation for the update method and add the mapping

### 5. UI Display Components

**`src/web/Jordnaer/Features/UserSearch/UserCard.razor`**
- Add visual indicator when user is actively looking:
```razor
@if (User.IsActiveLooking)
{
    <MudChip T="string" Variant="Variant.Filled" Color="Color.Success" Size="Size.Small">
        Søger aktivt
    </MudChip>
}
```
- Place this chip near the user's name or categories

**`src/web/Jordnaer/Features/Profile/UserProfileCard.razor`**
- Add the same visual indicator in the profile card
- Make it visible on the public profile page

**`src/web/Jordnaer/Pages/Profile/PublicProfile.razor`**
- Ensure the `IsActiveLooking` property is passed through to `UserProfileCard`

---

## Implementation Notes

### Existing Patterns to Follow
- Boolean flags with defaults: See `Partner.cs` which uses `public bool CanHaveAd { get; set; } = true;`
- Filter extensions: Follow the pattern in `QueryableGroupExtensions.cs` with `ApplyNameFilter` and `ApplyCategoryFilter`; same pattern exists in `QueryableUserProfileExtensions.cs`
- UI components: Use MudBlazor's `MudCheckBox` and `MudChip` components (already used throughout the codebase)
- Search filter equality: `UserSearchFilter` overrides `GetHashCode()` and `Equals()` — new properties must be included

### Danish Translations
- **Groups**:
  - "Vi søger aktivt efter nye medlemmer" = "We are actively looking for new members"
  - "Søger medlemmer" = "Looking for members"
  - "Kun grupper der søger medlemmer" = "Only groups looking for members"
- **Users**:
  - "Jeg søger aktivt efter nye kontakter" = "I am actively looking for new connections"
  - "Søger aktivt" = "Actively looking"
  - "Kun brugere der aktivt søger" = "Only users actively looking"

### Default Value
- Both `Group.IsActiveLookingForMembers` and `UserProfile.IsActiveLooking` default to `true`
- The migration should set default value `true` and backfill existing rows to `true`

### Testing Considerations
1. **Groups**:
   - Create a new group with the flag enabled/disabled — verify it persists
   - Edit an existing group to toggle the flag — verify update works
   - Search with the filter enabled — verify only matching groups appear
   - Verify the visual indicator displays correctly on GroupCard and GroupDetails
2. **Users**:
   - Edit profile to toggle the flag — verify update works
   - Search with the filter enabled — verify only matching users appear
   - Verify the visual indicator displays correctly on UserCard and UserProfileCard/PublicProfile
3. **Defaults**:
   - Verify new groups default to `IsActiveLookingForMembers = true`
   - Verify new users default to `IsActiveLooking = true`

---

## File Reference Summary

| File | Change Type |
|------|-------------|
| **Groups** | |
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
| **Users** | |
| `src/shared/Jordnaer.Shared/Database/UserProfile.cs` | Add property |
| `src/shared/Jordnaer.Shared/UserSearch/UserDto.cs` | Add property |
| `src/shared/Jordnaer.Shared/Profile/DTO/ProfileDto.cs` | Add property |
| `src/shared/Jordnaer.Shared/UserSearch/UserSearchFilter.cs` | Add filter property + update equality |
| `src/web/Jordnaer/Features/UserSearch/QueryableUserProfileExtensions.cs` | Add filter method |
| `src/web/Jordnaer/Features/UserSearch/UserSearchService.cs` | Apply filter + update mapping |
| `src/web/Jordnaer/Features/UserSearch/UserSearchForm.razor` | Add checkbox filter |
| `src/web/Jordnaer/Features/UserSearch/UserCard.razor` | Add visual indicator |
| `src/web/Jordnaer/Features/Profile/UserProfileCard.razor` | Add visual indicator |
| `src/web/Jordnaer/Pages/Profile/MyProfile.razor` | Add checkbox to edit form |
| `src/web/Jordnaer/Pages/Profile/PublicProfile.razor` | Ensure property flows through |
| **Migration** | |
| `src/web/Jordnaer/Database/Migrations/` | Single migration for both tables |
