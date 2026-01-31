# Upload Image Improvements

## Overview
Improve the profile image upload experience for users, child profiles, and groups by adding interactive cropping, guidance, and preview functionality.

## Current Implementation
- Uses `MudFileUpload` component with `RequestImageFileAsync()` for browser-side resizing to 200x200px
- Validation: JPEG/PNG only, max 2 MB
- Automatic square crop without user control
- Images stored in Azure Blob Storage
- Components: `UserProfilePicture.razor`, `ChildProfilePicture.razor`, `GroupProfilePicture.razor`

## Proposed Improvements

### 1. Interactive Crop Selection
**Goal:** Allow users to select which part of their image to use instead of automatic center-crop.

**Requirements:**
- Add a visual cropping interface after image selection
- Support drag-to-position and resize handles
- Maintain 1:1 aspect ratio (square) constraint
- Show crop preview in real-time
- Consider using a library like [Cropper.js](https://fengyuanchen.github.io/cropperjs/) via JS interop or a Blazor wrapper

**Affected files:**
- [UserProfilePicture.razor](src/web/Jordnaer/Features/Images/UserProfilePicture.razor)
- [ChildProfilePicture.razor](src/web/Jordnaer/Features/Images/ChildProfilePicture.razor)
- [GroupProfilePicture.razor](src/web/Jordnaer/Features/Groups/GroupProfilePicture.razor)

### 2. Upload Guide / Instructions
**Goal:** Help users understand what makes a good profile picture.

**Requirements:**
- Display guidance text near the upload button explaining:
  - Recommended image dimensions (at least 400x400px for quality)
  - Accepted formats (JPEG, PNG)
  - Maximum file size (2 MB)
  - Tips for good profile photos (clear face, good lighting, appropriate for families)
- Show guidance in a collapsible section or tooltip to avoid clutter
- Use Danish text consistent with existing localization

### 3. Size & Quality Advice
**Goal:** Warn users before upload if their image may have quality issues.

**Requirements:**
- After image selection, analyze the source image dimensions
- Show warning if image is:
  - Too small (< 200x200px) - will appear pixelated
  - Very large (> 4000x4000px) - may take longer to process
  - Wrong aspect ratio - explain cropping will occur
- Recommend optimal size range (400x400 to 2000x2000px)
- Allow upload anyway with acknowledgment

### 4. Preview How Others Will See Your Profile
**Goal:** Show users exactly how their profile picture will appear in different contexts.

**Requirements:**
- After cropping, show preview mockups of:
  - Profile card view (150x150px circular, as shown in search results)
  - Small avatar view (40x40px, as shown in chat/messages)
  - Full profile page view (250x250px)
- Use actual components (`UserProfileCard`, etc.) with the new image for realistic preview
- Include "Sådan vil dit billede se ud" (This is how your image will look) header

## Technical Considerations

- **JS Interop:** Cropping library will likely require JavaScript interop
- **State Management:** Keep original image in memory until user confirms crop
- **Performance:** Process cropping client-side to avoid server round-trips
- **Mobile:** Ensure touch-friendly crop controls
- **Accessibility:** Keyboard navigation for crop selection

## Out of Scope
- Video uploads
- Animated GIF support
- AI-based auto-enhancement
- Background removal

## Acceptance Criteria
- [ ] Users can interactively select crop area before upload
- [ ] Clear guidance is displayed about image requirements
- [ ] Warnings shown for suboptimal image sizes
- [ ] Preview shows how image will appear in different contexts
- [ ] All three profile types (user, child, group) support new features
- [ ] Works on desktop and mobile browsers
- [ ] Danish text for all user-facing strings
