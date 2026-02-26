# Improve Colors

## Problem 1: Error color is hard to read

The `Error` palette color is `#673417` (a dark red-brown), which is too dark and low-contrast
for use as foreground text/icons on a white background. It is used as:

- Inline asterisks marking required fields: `style="color: var(--mud-palette-error);"` in
  [Pages/Groups/CreateGroup.razor](../src/web/Jordnaer/Pages/Groups/CreateGroup.razor:39)
- Validation error text: `Color="Color.Error"` in [Pages/Groups/CreateGroup.razor](../src/web/Jordnaer/Pages/Groups/CreateGroup.razor:70)
- Close icon color: `CloseIconStyle="color: var(--mud-palette-error)"` in
  [Features/Profile/EditChildProfileTabs.razor](../src/web/Jordnaer/Features/Profile/EditChildProfileTabs.razor:8)
- Error state text colors on the Register page (password requirements)
- Various destructive-action buttons (`Color.Error`)

**Fix:** Change `Error` in `PaletteLight` in
[Features/Theme/JordnaerPalette.cs](../src/web/Jordnaer/Features/Theme/JordnaerPalette.cs:36)
to a brighter, clearly readable red. The current value is `JordnaerPalette.RedHeader` (`#673417`).
Replace it with a vivid red such as `#c0392b` or `#d32f2f` that reads clearly on white.
Also add a new named constant for this in `JordnaerPalette` (e.g. `ErrorRed = "#c0392b"`) and
reference it from the palette definition.

**Verify:** After the change, `--mud-palette-error` in the browser DevTools should resolve to the
new brighter red. Required-field asterisks and validation messages on
`/groups/create` should be clearly visible in red.

---

## Problem 2: Category chips are boring

Category chips currently render in a flat, muted style:

- **[CategorySelector.razor](../src/web/Jordnaer/Features/Category/CategorySelector.razor)**
  (used when creating/editing a group or profile):
  - Unselected chips: solid `#6b7280` (gray) background with white text.
  - Selected chips: solid `var(--mud-palette-success)` (`#878e64`, muted olive green) background.
  - Both use `Variant.Filled` with overriding CSS classes.

- **[GroupCard.razor](../src/web/Jordnaer/Features/GroupSearch/GroupCard.razor:71-82)**
  (shown in search results):
  - Outlined chips with `border-color` and `color` both set to `JordnaerPalette.GreenBackground`
    (`#878e64`) via inline style.

- **[UserProfileCard.razor](../src/web/Jordnaer/Features/Profile/UserProfileCard.razor:50)**
  (shown on user profile pages):
  - Filled chips with `Color="Color.Success"` (`#878e64`).

**Fix:** Make the chips visually interesting using the brand palette. Suggested approach — adjust
to taste, but the result must be clearly more colourful/distinctive than the current grey/olive:

1. **CategorySelector.razor** — Change unselected chips to use `JordnaerPalette.PaleBlueBackground`
   (`#a9c0cf`) as background, and selected chips to use `JordnaerPalette.YellowBackground`
   (`#dbab45`) with dark text (`JordnaerPalette.BlueBodyDark`, `#2d4456`).
   Update the CSS classes accordingly and set `Color` attributes to `Color.Default` (so MudBlazor
   doesn't fight the custom CSS).

2. **GroupCard.razor** — Change category chips from outlined green to filled chips using
   `JordnaerPalette.PaleBlueBackground` (`#a9c0cf`) background and `JordnaerPalette.BlueBodyDark`
   (`#2d4456`) text. Remove the inline `Style` and instead use `Color="Color.Tertiary"` (which is
   already mapped to `PaleBlueBackground` in the theme) with `Variant.Filled`.

3. **UserProfileCard.razor** — Change `Color="Color.Success"` to `Color="Color.Tertiary"` and
   `Variant="Variant.Filled"`, matching the GroupCard chips for visual consistency.

**Verify:**
- On `/groups/create` or `/profile/edit`, the category selector should show blue (unselected) and
  yellow (selected) chips instead of gray/green.
- On the group search page (`/groups`), category chips on each group card should show in pale blue
  rather than outlined olive green.
- On a user profile page, category chips should match the pale blue filled style.
- No chip text should be illegible (check contrast: dark text on pale blue, white or dark text on
  yellow).
