# Task 09: Enhanced Registration - Profile Completion Onboarding

## Context

**App:** Jordnaer (.NET Blazor Server)
**Area:** Registration / Onboarding
**Priority:** High
**Impact:** User search quality, engagement, retention

## Objective

Improve user profile completeness at registration time by adding a profile completion step to the onboarding flow. This ensures new users have searchable, meaningful profiles from day one, leading to better search results and faster connection-making.

## Problem Statement

**Current State:**

- Users register with only email and password ([Register.razor:161-164](src/web/Jordnaer/Components/Account/Pages/Register.razor#L161-L164))
- Empty UserProfile created: `new UserProfile { Id = user.Id }`
- Critical fields are missing: name, location, interests, description
- Users must manually navigate to `/profile` to complete their profile
- **Many users never complete their profile** → poor search results → reduced engagement

**Impact:**

- Incomplete profiles don't appear in meaningful searches
- Users searching for connections find empty/minimal results
- Poor first impression ("this app has no active users")
- Lower engagement and retention

## Solution Overview

Add a **multi-step onboarding flow** after account creation that collects essential profile information before users enter the main app.

### Flow Comparison

**Current Flow:**

```
Register (email/password) → Email Confirmation → Home (incomplete profile)
```

**New Flow:**

```
Register (email/password) → Email Confirmation → Profile Setup (Step 1/2) → Profile Setup (Step 2/2) → Home (complete profile)
```

Make profile setup **required** for best results, but show progress (e.g., "Step 1 of 2") to set expectations.

## Detailed Requirements

### Step 1: Basic Info & Location (Required)

**Fields to Collect:**

1. **First Name** (required)
2. **Last Name** (required)
3. **Date of Birth** (optional but recommended)
4. **Location** (required - address or zip code)
   - Toggle between full address and zip code only (privacy)
   - Use existing `AddressAutoComplete` and `ZipCodeAutoComplete` components

**Auto-Generated Username:**

- Generate username from `{firstname}{lastname}` (e.g., "nielspilgaard")
- Check for uniqueness, append number if taken (e.g., "nielspilgaard2")
- User can edit username later in profile settings
- Show preview: "Your profile will be at: mini-moeder.dk/nielspilgaard"

**Why These Fields:**

- Name: Essential for identity and search
- Location: Core to Jordnaer's location-based matching
- Date of Birth: Age-based filtering and life stage matching
- Username: Makes profile publicly accessible

### Step 2: Interests & Introduction (Recommended)

**Fields to Collect:**

1. **Categories/Interests** (select multiple)

   - Use existing `CategorySelector` component
   - "What are you interested in?" prompt
   - At least 1 category recommended

2. **Short Introduction** (optional)
   - Use existing `TextEditorComponent` (WYSIWYG)
   - Placeholder: "Tell others a bit about yourself - your interests, family, values, what you're looking for in connections..."
   - Character limit suggestion: 500-1000 chars for onboarding

**Why These Fields:**

- Categories: Enable interest-based search and matching
- Description: Gives personality to the profile, encourages engagement

### Implementation Details

#### Create New Pages

**1. `/Account/CompleteProfile` - Step 1 (Basic Info)**

```razor
@page "/Account/CompleteProfile"
@attribute [Authorize]

<MetadataComponent Title="Fuldfør din profil - Grundlæggende info" />

<div class="account-container">
    <div class="account-paper">
        <h1 class="account-header">Velkommen til Jordnaer! 🎉</h1>
        <p class="text-center">Lad os oprette din profil, så andre kan finde dig</p>

        <!-- Progress indicator -->
        <MudStepper @ref="_stepper" ActiveIndex="0">
            <MudStep Icon="@Icons.Material.Filled.Person">Grundlæggende info</MudStep>
            <MudStep Icon="@Icons.Material.Filled.Interests">Interesser</MudStep>
        </MudStepper>

        <EditForm Model="@_profileInput" OnValidSubmit="SaveBasicInfo">
            <DataAnnotationsValidator />

            <!-- First Name -->
            <div class="form-floating">
                <InputText @bind-Value="_profileInput.FirstName"
                           class="form-control"
                           placeholder="Fornavn"
                           id="firstname" autofocus />
                <label for="firstname">Fornavn *</label>
                <ValidationMessage For="() => _profileInput.FirstName" />
            </div>

            <!-- Last Name -->
            <div class="form-floating">
                <InputText @bind-Value="_profileInput.LastName"
                           class="form-control"
                           placeholder="Efternavn"
                           id="lastname" />
                <label for="lastname">Efternavn *</label>
                <ValidationMessage For="() => _profileInput.LastName" />
            </div>

            <!-- Username Preview (auto-generated) -->
            @if (!string.IsNullOrWhiteSpace(_generatedUsername))
            {
                <div class="alert alert-info">
                    <MudIcon Icon="@Icons.Material.Filled.Link" Size="Size.Small" />
                    Din profil vil være tilgængelig på:
                    <strong>mini-moeder.dk/@_generatedUsername</strong>
                    <br />
                    <small>Du kan ændre dit brugernavn senere i indstillinger</small>
                </div>
            }

            <!-- Date of Birth -->
            <MudDatePicker @bind-Date="_profileInput.DateOfBirth"
                           Label="Fødselsdato (valgfrit)"
                           Placeholder="Vælg dato"
                           OpenTo="OpenTo.Year"
                           Variant="Variant.Outlined"
                           Class="mb-3" />

            <!-- Location Section -->
            <MudText Typo="Typo.h6" Class="mb-3">Hvor bor du? *</MudText>

            @if (_useZipCodeOnly)
            {
                <ZipCodeAutoComplete Location="@_zipCodeLocation"
                                     LocationChanged="OnZipCodeLocationChanged" />
            }
            else
            {
                <AddressAutoComplete Address="@_addressLocation"
                                     AddressChanged="OnAddressLocationChanged" />
            }

            <!-- Privacy Toggle -->
            <MudTooltip Text="Klik for at skifte mellem fuld adresse og kun postnummer">
                <MudChip T="string"
                         Color="@(_useZipCodeOnly ? Color.Default : Color.Info)"
                         Variant="Variant.Outlined"
                         OnClick="ToggleLocationMode"
                         Style="cursor: pointer;">
                    @if (_useZipCodeOnly)
                    {
                        <MudIcon Icon="@Icons.Material.Filled.Lock" Size="Size.Small" />
                        <span>Kun postnummer (mere privat)</span>
                    }
                    else
                    {
                        <MudIcon Icon="@Icons.Material.Filled.LocationOn" Size="Size.Small" />
                        <span>Fuld adresse</span>
                    }
                </MudChip>
            </MudTooltip>

            <MudAlert Severity="Severity.Info" Variant="Variant.Text" Dense Class="my-3">
                Din adresse vises aldrig offentligt. Andre ser kun omtrentlig afstand (f.eks. "5 km væk").
            </MudAlert>

            <!-- Navigation Buttons -->
            <div class="d-flex justify-space-between mt-4">
                <MudButton Variant="Variant.Text"
                           Href="/Account/Manage"
                           Color="Color.Default">
                    Spring over (gør det senere)
                </MudButton>

                <MudButton ButtonType="ButtonType.Submit"
                           Variant="Variant.Filled"
                           Color="Color.Info"
                           EndIcon="@Icons.Material.Filled.ArrowForward">
                    Næste
                </MudButton>
            </div>
        </EditForm>
    </div>
</div>
```

**2. `/Account/CompleteProfileInterests` - Step 2 (Interests)**

```razor
@page "/Account/CompleteProfileInterests"
@attribute [Authorize]

<MetadataComponent Title="Fuldfør din profil - Interesser" />

<div class="account-container">
    <div class="account-paper">
        <h1 class="account-header">Hvad er du interesseret i?</h1>
        <p class="text-center">Dette hjælper andre med at finde dig baseret på fælles interesser</p>

        <!-- Progress indicator -->
        <MudStepper @ref="_stepper" ActiveIndex="1">
            <MudStep Icon="@Icons.Material.Filled.Person">Grundlæggende info</MudStep>
            <MudStep Icon="@Icons.Material.Filled.Interests">Interesser</MudStep>
        </MudStepper>

        <EditForm Model="@_interestsInput" OnValidSubmit="SaveInterestsAndComplete">
            <!-- Category Selector -->
            <MudText Typo="Typo.body1" Class="mb-2">Vælg kategorier</MudText>
            <CategorySelector @bind-Categories="_selectedCategoryNames" />

            <MudAlert Severity="Severity.Info" Variant="Variant.Text" Dense Class="my-3">
                Vælg mindst én kategori for de bedste resultater
            </MudAlert>

            <!-- Description -->
            <MudText Typo="Typo.body1" Class="mb-2 mt-4">
                Fortæl lidt om dig selv (valgfrit)
            </MudText>
            <TextEditorComponent
                Placeholder="Du kan f.eks fortælle om dine interesser, familie, værdier og hvad du søger i forbindelser..."
                Label="Introduktion"
                @ref="_textEditorComponent" />

            <!-- Navigation Buttons -->
            <div class="d-flex justify-space-between mt-4">
                <MudButton Variant="Variant.Text"
                           Href="/Account/CompleteProfile"
                           StartIcon="@Icons.Material.Filled.ArrowBack">
                    Tilbage
                </MudButton>

                <MudButton ButtonType="ButtonType.Submit"
                           Variant="Variant.Filled"
                           Color="Color.Success"
                           EndIcon="@Icons.Material.Filled.Check">
                    Fuldfør profil
                </MudButton>
            </div>

            <MudButton Variant="Variant.Text"
                       Href="/"
                       Color="Color.Default"
                       Class="mt-2 w-100">
                Spring over (gør det senere)
            </MudButton>
        </EditForm>
    </div>
</div>
```

#### Modify Registration Flow

**Update `Register.razor` (Line 182):**

```csharp
// OLD:
await SignInManager.SignInAsync(user, isPersistent: false);
RedirectManager.RedirectTo(ReturnUrl);

// NEW:
await SignInManager.SignInAsync(user, isPersistent: false);

// Redirect to profile completion instead of home
RedirectManager.RedirectTo("Account/CompleteProfile");
```

**Alternative (if email confirmation is required):**

Update `ConfirmEmail.razor` to redirect to `/Account/CompleteProfile` after confirmation instead of home.

#### Backend Service Updates

**Update `ProfileService.cs`:**

Add method to check if profile is complete:

```csharp
public async Task<bool> IsProfileCompleteAsync(string userId)
{
    var profile = await GetUserProfileAsync(userId);

    if (profile is null) return false;

    // Basic completeness check
    return !string.IsNullOrWhiteSpace(profile.FirstName) &&
           !string.IsNullOrWhiteSpace(profile.LastName) &&
           profile.Location is not null &&
           (!string.IsNullOrWhiteSpace(profile.ZipCode) || !string.IsNullOrWhiteSpace(profile.Address));
}

public async Task<Result<string>> GenerateUniqueUsernameAsync(string firstName, string lastName)
{
    var baseUsername = $"{firstName}{lastName}".ToLowerInvariant()
        .Replace(" ", "")
        .Replace("æ", "ae")
        .Replace("ø", "oe")
        .Replace("å", "aa");

    // Remove non-alphanumeric characters
    baseUsername = new string(baseUsername.Where(c => char.IsLetterOrDigit(c)).ToArray());

    if (string.IsNullOrWhiteSpace(baseUsername))
    {
        return Result<string>.Failure("Kunne ikke generere brugernavn");
    }

    var username = baseUsername;
    var counter = 1;

    // Check uniqueness
    while (await _context.UserProfiles.AnyAsync(p => p.UserName == username))
    {
        counter++;
        username = $"{baseUsername}{counter}";

        if (counter > 1000) // Safety valve
        {
            return Result<string>.Failure("Kunne ikke finde et unikt brugernavn");
        }
    }

    return Result<string>.Success(username);
}
```

### Design Considerations

#### Visual Design

- Use warm colors from `JordnaerPalette` (GLÆDE yellow, RO green)
- Apply `.warm-shadow`, `.warm-rounded` classes
- Maintain consistency with existing account pages (use `.account-container`, `.account-paper`)
- Show progress clearly (MudStepper or custom progress bar)

#### UX Considerations

- **Required vs Optional:** Mark required fields with `*`
- **Skip Option:** Allow users to skip, but show gentle reminder
- **Save Progress:** Auto-save to prevent data loss (or use browser storage)
- **Mobile-Friendly:** Ensure forms work well on mobile devices
- **Error Handling:** Clear validation messages
- **Loading States:** Show loading spinner during save operations

#### Privacy & Trust

- Emphasize that address is never shown publicly
- Show the privacy toggle (zip code vs full address)
- Explain why each field is collected
- Link to privacy policy if available

### User Experience Flow

**Scenario 1: Compliant User**

1. User registers with email/password
2. Confirms email
3. Redirected to "Complete Profile" step 1
4. Fills in name, DOB, location (takes ~1 minute)
5. Clicks "Next"
6. Selects interests and writes intro (takes ~2 minutes)
7. Clicks "Finish" → Redirected to home with complete profile ✅

**Scenario 2: Impatient User**

1. User registers
2. Confirms email
3. Sees "Complete Profile" page
4. Clicks "Skip for now"
5. Lands on home page
6. Later sees prompt/banner: "Complete your profile to be discoverable"
7. Can complete from `/profile` page

### Data Validation

**First Name:**

- Required
- Min length: 2 characters
- Max length: 50 characters
- Pattern: Letters, spaces, hyphens only

**Last Name:**

- Required
- Min length: 2 characters
- Max length: 50 characters
- Pattern: Letters, spaces, hyphens only

**Username:**

- Auto-generated, but editable later
- Must be unique
- 3-30 characters
- Alphanumeric, dash and underscores only
- Cannot start with number

**Location:**

- Either Address OR (ZipCode + City) required
- Validates using `LocationService`

**Date of Birth:**

- Optional
- Must be in the past
- User must be at least 18 years old (COPPA compliance)

**Categories:**

- Recommended (at least 1)
- No maximum limit

**Description:**

- Optional
- Max length: 2000 characters
- HTML sanitized

## Acceptance Criteria

- [ ] New registration flow redirects to `/Account/CompleteProfile` after email confirmation
- [ ] Step 1 page collects: first name, last name, DOB, location
- [ ] Username is auto-generated from first + last name
- [ ] Username uniqueness is validated
- [ ] Username preview shows "mini-moeder.dk/{username}"
- [ ] Location can be entered as full address or zip code only
- [ ] Privacy toggle works (address ↔ zip code)
- [ ] Privacy message displayed ("Your address is never shown publicly")
- [ ] Step 2 page collects: categories/interests and description
- [ ] CategorySelector component is reused
- [ ] TextEditorComponent (WYSIWYG) is reused for description
- [ ] Progress indicator shows current step (1 of 2, 2 of 2)
- [ ] "Skip" button available on both steps
- [ ] "Back" button on step 2 returns to step 1
- [ ] Profile is saved to database after step 1 (basic info)
- [ ] Profile is updated after step 2 (interests)
- [ ] Middleware redirects incomplete profiles to completion flow (optional)
- [ ] Validation messages are clear and helpful
- [ ] Forms work on mobile devices
- [ ] Loading states shown during save operations
- [ ] After completion, user is redirected to home page
- [ ] Completed profiles are searchable
- [ ] Design matches existing account pages (warm colors, consistent styling)

## Files to Create

**New Pages:**

- `src/web/Jordnaer/Components/Account/Pages/CompleteProfile.razor` (Step 1)
- `src/web/Jordnaer/Components/Account/Pages/CompleteProfileInterests.razor` (Step 2)

## Files to Modify

**Registration Flow:**

- `src/web/Jordnaer/Components/Account/Pages/Register.razor` (line 182 - change redirect)
- OR `src/web/Jordnaer/Components/Account/Pages/ConfirmEmail.razor` (if email confirmation required)

**Services:**

- `src/web/Jordnaer/Features/Profile/ProfileService.cs`
  - Add `IsProfileCompleteAsync` method
  - Add `GenerateUniqueUsernameAsync` method

## Existing Components to Reuse

✅ Already exists - no need to recreate:

- `AddressAutoComplete` - for full address input
- `ZipCodeAutoComplete` - for zip code only input
- `CategorySelector` - for interests selection
- `TextEditorComponent` - for WYSIWYG description
- `LocationService` - for geocoding and location validation

## Implementation Order

1. **Create Backend Services** (30 minutes)

   - Add `IsProfileCompleteAsync` to ProfileService
   - Add `GenerateUniqueUsernameAsync` to ProfileService
   - Test username generation logic

2. **Create Step 1 Page** (1-2 hours)

   - Create `CompleteProfile.razor`
   - Implement basic info form (name, DOB, location)
   - Implement username generation with preview
   - Implement location privacy toggle
   - Test validation and save

3. **Create Step 2 Page** (1 hour)

   - Create `CompleteProfileInterests.razor`
   - Integrate CategorySelector
   - Integrate TextEditorComponent
   - Test save and redirect

4. **Update Registration Flow** (30 minutes)

   - Modify Register.razor redirect
   - Test end-to-end registration flow

5. **Polish & Testing** (1-2 hours)
   - Apply design system styles
   - Test mobile responsiveness
   - Add progress indicators
   - Test skip functionality
   - Test error states
   - User acceptance testing

**Total Estimated Time:** 5-7 hours

## Testing Checklist

- [ ] New user can complete profile in onboarding flow
- [ ] Username is auto-generated correctly
- [ ] Username handles special characters (æ, ø, å)
- [ ] Username uniqueness check works
- [ ] Duplicate usernames get numbered suffix (username2, username3, etc.)
- [ ] Location services work for both address and zip code
- [ ] Privacy toggle switches between address and zip code
- [ ] Location data is saved correctly (ZipCode, City, Location geometry)
- [ ] Categories are saved correctly
- [ ] Description HTML is sanitized
- [ ] Skip button redirects to home
- [ ] Back button works on step 2
- [ ] Profile completion is saved after each step
- [ ] Middleware redirects incomplete profiles (if enabled)
- [ ] Forms are responsive on mobile
- [ ] Validation errors are clear
- [ ] Loading states appear during saves
- [ ] Profile is searchable after completion
- [ ] Users can edit their profile later at `/profile`

## Notes

- Consider adding onboarding tooltips/hints for first-time users
- Could add gamification: "Your profile is 75% complete!"
- Could send email reminder to users with incomplete profiles
- Consider adding profile completion indicator in header/sidebar
- Could add social proof: "Users with complete profiles get 3x more connections"
- GDPR compliance: Ensure users can skip and delete data later
- Consider adding "Why we ask this" tooltips for each field
- Mobile-first design: Most users register on mobile

## Privacy Considerations

- Address is never displayed publicly (emphasized multiple times)
- Zip code option for extra privacy
- Date of birth can be used for age ranges, not exact age display
- Username makes profile public - user must understand this
- Clear explanation of how location data is used ("Others see only distance")
- Link to privacy policy on onboarding pages

## Future Enhancements

- Upload profile picture during onboarding
- Connect social accounts (optional)
- Import interests from Facebook/LinkedIn
- Video introduction (short selfie video)
- Profile preview before publishing
- Onboarding tutorial/tour after completion
- Buddy system: match new users with ambassadors

## 🚨 CRITICAL: Read SSR Constraints First

**⚠️ IMPORTANT: Before implementing, read [09-CRITICAL-SSR-CONSTRAINTS.md](09-CRITICAL-SSR-CONSTRAINTS.md)**

Pages in `/Account` use Blazor Static SSR (no `@onclick`, no client-side interactivity).
This heavily impacts implementation. See the constraints document for:

- What you cannot use (interactive components, @onclick, etc.)
- What you must use instead (form submissions, radio buttons, etc.)
- Complete SSR-compatible code examples
- Alternative: Move onboarding to `/profile/onboarding` (interactive) instead

## External Authentication (Google, Facebook, Microsoft)

### Current External Login Behavior

**What's Already Populated:**
From [ClaimsPrincipalExtensions.cs:7-34](src/shared/Jordnaer.Shared/Extensions/ClaimsPrincipalExtensions.cs#L7-L34):

- ✅ FirstName (from ClaimTypes.GivenName or ClaimTypes.Name)
- ✅ LastName (from ClaimTypes.Surname or ClaimTypes.Name)
- ⚠️ UserName (concatenated from first+last, **but NOT checked for uniqueness!**)

**Still Missing:**

- ❌ Location (address/zip code)
- ❌ DateOfBirth
- ❌ Categories/interests
- ❌ Description

### Required Changes

**1. Update ExternalLogin.razor - New User Flow**

[ExternalLogin.razor:189](src/web/Jordnaer/Components/Account/Pages/ExternalLogin.razor#L189) currently redirects new external users to home:

```csharp
// OLD (Line 189):
await SignInManager.SignInAsync(user, isPersistent: false, _externalLoginInfo.LoginProvider);
RedirectManager.RedirectTo(FirstLoginReturnUrl);

// NEW:
await SignInManager.SignInAsync(user, isPersistent: false, _externalLoginInfo.LoginProvider);
// Redirect to profile completion instead of home
RedirectManager.RedirectTo("/Account/CompleteProfile");
```

**2. Update ExternalLogin.razor - Existing User Flow**

[ExternalLogin.razor:132](src/web/Jordnaer/Components/Account/Pages/ExternalLogin.razor#L132) redirects existing users to home. Should check if profile is complete:

```csharp
// OLD (Line 132):
RedirectManager.RedirectTo(FirstLoginReturnUrl);

// NEW:
// Check if profile is complete (optional - middleware will handle this)
var isProfileComplete = await ProfileService.IsProfileCompleteAsync(user.Id);
if (!isProfileComplete)
{
    RedirectManager.RedirectTo("/Account/CompleteProfile");
}
else
{
    RedirectManager.RedirectTo(FirstLoginReturnUrl);
}
```

**Note:** If using ProfileCompletionMiddleware, the check on line 132 is optional as middleware will redirect incomplete profiles anyway.

**3. Fix Username Uniqueness Issue**

The `ToUserProfile()` extension creates usernames without checking uniqueness. This needs fixing:

**Option A:** Pre-generate username before creating profile

```csharp
// In ExternalLogin.razor OnValidSubmitAsync (after line 167)
var userProfile = _externalLoginInfo!.Principal.ToUserProfile(user.Id);

// Generate unique username
var usernameResult = await ProfileService.GenerateUniqueUsernameAsync(
    userProfile.FirstName ?? "user",
    userProfile.LastName ?? "");

if (usernameResult.IsSuccess)
{
    userProfile.UserName = usernameResult.Value;
}

Context.UserProfiles.Add(userProfile);
await Context.SaveChangesAsync();
```

**Option B:** Let CompleteProfile page handle username generation

- Remove username assignment from ToUserProfile()
- CompleteProfile page generates username from existing FirstName/LastName

### CompleteProfile Page Behavior for External Users

When external users arrive at CompleteProfile:

**Step 1: Basic Info**

- FirstName: Pre-populated (read-only or editable)
- LastName: Pre-populated (read-only or editable)
- Username: Auto-generated from existing FirstName/LastName (shown as preview)
- DateOfBirth: Empty (user fills in)
- Location: Empty (user fills in - required)

**Step 2: Interests**

- Categories: Empty (user selects)
- Description: Empty (user writes)

### Modified Acceptance Criteria

Add these criteria for external authentication:

- [ ] New external login users redirect to `/Account/CompleteProfile` after signup
- [ ] Existing external users with incomplete profiles redirect to CompleteProfile (via middleware or check)
- [ ] External users see pre-populated FirstName/LastName in CompleteProfile
- [ ] Username uniqueness is guaranteed for external users
- [ ] External users can complete profile without re-entering name
- [ ] All three providers (Google, Facebook, Microsoft) work correctly

### Files to Modify (External Auth)

**Registration Flow:**

- `src/web/Jordnaer/Components/Account/Pages/ExternalLogin.razor`
  - Line 189: Redirect new users to CompleteProfile
  - Line 132: (Optional) Check profile completeness for existing users
  - After line 167: Ensure username uniqueness

**Extensions:**

- `src/shared/Jordnaer.Shared/Extensions/ClaimsPrincipalExtensions.cs`
  - Consider removing username assignment (let CompleteProfile handle it)
  - Or keep for backward compatibility, fix in ExternalLogin.razor

### Testing Checklist (External Auth)

- [ ] New Google user completes profile successfully
- [ ] New Facebook user completes profile successfully
- [ ] New Microsoft user completes profile successfully
- [ ] External users have unique usernames (no duplicates)
- [ ] External users can skip profile completion (if skip enabled)
- [ ] Existing external users with incomplete profiles are redirected
- [ ] Pre-populated FirstName/LastName are correct from claims
- [ ] Username is generated correctly from external claims
- [ ] Location and categories are saved for external users
- [ ] External users appear in search after completing profile

### External Provider Claim Mapping

Different providers send different claims:

| Provider  | First Name Claim     | Last Name Claim    | Full Name Claim |
| --------- | -------------------- | ------------------ | --------------- |
| Google    | ClaimTypes.GivenName | ClaimTypes.Surname | ClaimTypes.Name |
| Microsoft | ClaimTypes.GivenName | ClaimTypes.Surname | ClaimTypes.Name |
| Facebook  | (custom claim)       | (custom claim)     | ClaimTypes.Name |

Current `ToUserProfile()` handles this with fallback logic:

1. Try to get full name, split on space
2. Fall back to GivenName + Surname claims

This should work for all three providers, but test thoroughly.

### Potential Issues

**Issue 1: Duplicate Usernames**

- External users might have same first+last name as existing users
- **Solution:** Use `GenerateUniqueUsernameAsync` to append numbers

**Issue 2: Missing Claims**

- Some providers might not send name claims
- **Solution:** CompleteProfile page should allow editing name fields even if pre-populated

**Issue 3: Skip Button**

- External users might skip, leaving incomplete profile
- **Solution:** Middleware redirects them back until complete (or allow skip with banner reminder)
