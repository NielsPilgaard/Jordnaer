# ⚠️ CRITICAL: Blazor Rendering Mode Constraints for Task 09

**This is a critical addendum to Task 09 (Onboarding Profile Completion)**

## The Problem

Pages in `/Account` use **Blazor Static SSR** (Server-Side Rendering without interactivity).
All other pages (`/profile`, `/posts`, etc.) use **Blazor Server** with full interactivity.

## Rendering Mode Comparison

| Location | Rendering Mode | Interactivity | Example Pages |
|----------|----------------|---------------|---------------|
| `/Account/*` | **Static SSR** | ❌ None - Form submissions only | Register, Login, Manage |
| All other pages | **Blazor Server** | ✅ Full - `@onclick`, real-time updates | MyProfile, Posts, Groups |

## What You CANNOT Do in `/Account/CompleteProfile`

**❌ These will NOT work in Static SSR:**

```razor
@* ❌ BAD: @onclick doesn't work in SSR *@
<MudButton @onclick="ToggleLocationMode">Toggle</MudButton>

@* ❌ BAD: Client-side state changes won't persist *@
@code {
    private bool _useZipCode = false;
    private void ToggleLocationMode() => _useZipCode = !_useZipCode; // Won't work!
}

@* ❌ BAD: Interactive stepper requires @onclick *@
<MudStepper ActiveStepChanged="OnStepChanged">...</MudStepper>

@* ❌ BAD: JavaScript interop won't work directly *@
await JS.InvokeVoidAsync("scrollTo", 0, 0);

@* ❌ BAD: Real-time validation without form submission *@
<input @oninput="ValidateImmediately" /> // Won't work
```

## What You MUST Do Instead

**✅ Use these SSR-compatible patterns:**

```razor
@* ✅ GOOD: Use EditForm with method="post" *@
<EditForm Model="Input" method="post" OnValidSubmit="HandleSubmit" FormName="complete-profile">
    <DataAnnotationsValidator />

    @* Fields here *@

    <button type="submit">Next</button>
</EditForm>

@code {
    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    private async Task HandleSubmit()
    {
        // Save to database
        // Redirect to next page
        RedirectManager.RedirectTo("/Account/CompleteProfileInterests");
    }
}

@* ✅ GOOD: Radio buttons for toggle (SSR-friendly) *@
<InputRadioGroup @bind-Value="Input.LocationType">
    <label>
        <InputRadio Value="@("address")" />
        📍 Fuld adresse
    </label>
    <label>
        <InputRadio Value="@("zipcode")" />
        🔒 Kun postnummer
    </label>
</InputRadioGroup>

@* ✅ GOOD: Conditional rendering based on form state *@
@if (Input.LocationType == "address")
{
    <div class="form-floating">
        <InputText @bind-Value="Input.Address" class="form-control" />
        <label>Adresse</label>
    </div>
}
else
{
    <div class="form-floating">
        <InputText @bind-Value="Input.ZipCode" class="form-control" />
        <label>Postnummer</label>
    </div>
}

@* ✅ GOOD: Simple link for skip (no @onclick needed) *@
<a href="/" class="btn btn-link">Spring over</a>

@* ✅ GOOD: CSS-only progress indicator *@
<div class="progress-bar">
    <div class="progress-step active">1</div>
    <div class="progress-step">2</div>
</div>
```

## Recommended Implementation for Two-Step Flow

**Use Two Separate Pages** (Cleanest approach for SSR):

### Page 1: `/Account/CompleteProfile`
```razor
@page "/Account/CompleteProfile"
@attribute [Authorize]
@inject IdentityRedirectManager RedirectManager
@inject IProfileService ProfileService

<EditForm Model="Input" method="post" OnValidSubmit="SaveBasicInfo" FormName="basic-info">
    <DataAnnotationsValidator />

    <h1>Velkommen! Step 1 of 2</h1>

    <div class="form-floating">
        <InputText @bind-Value="Input.FirstName" class="form-control" />
        <label>Fornavn</label>
        <ValidationMessage For="() => Input.FirstName" />
    </div>

    <div class="form-floating">
        <InputText @bind-Value="Input.LastName" class="form-control" />
        <label>Efternavn</label>
        <ValidationMessage For="() => Input.LastName" />
    </div>

    @* Location choice via radio buttons *@
    <InputRadioGroup @bind-Value="Input.LocationType">
        <label>
            <InputRadio Value="@("address")" />
            📍 Fuld adresse
        </label>
        <label>
            <InputRadio Value="@("zipcode")" />
            🔒 Kun postnummer
        </label>
    </InputRadioGroup>

    @* Show appropriate field *@
    @if (Input.LocationType == "address")
    {
        <div class="form-floating">
            <InputText @bind-Value="Input.Address" class="form-control" />
            <label>Adresse</label>
        </div>
    }
    else
    {
        <div class="form-floating">
            <InputText @bind-Value="Input.ZipCode" class="form-control" />
            <label>Postnummer</label>
        </div>
    }

    <button type="submit" class="btn btn-primary">Næste</button>
    <a href="/" class="btn btn-link">Spring over</a>
</EditForm>

@code {
    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    private async Task SaveBasicInfo()
    {
        // 1. Generate username from first + last name
        var username = await ProfileService.GenerateUniqueUsernameAsync(
            Input.FirstName,
            Input.LastName);

        // 2. Save to database (partial profile)
        await ProfileService.UpdateBasicInfoAsync(new UserProfile
        {
            FirstName = Input.FirstName,
            LastName = Input.LastName,
            UserName = username.Value,
            // ... location fields
        });

        // 3. Redirect to step 2
        RedirectManager.RedirectTo("/Account/CompleteProfileInterests");
    }

    private sealed class InputModel
    {
        [Required] public string FirstName { get; set; } = "";
        [Required] public string LastName { get; set; } = "";
        public string LocationType { get; set; } = "zipcode"; // Default to more private
        public string? Address { get; set; }
        public string? ZipCode { get; set; }
    }
}
```

### Page 2: `/Account/CompleteProfileInterests`
```razor
@page "/Account/CompleteProfileInterests"
@attribute [Authorize]
@inject IdentityRedirectManager RedirectManager
@inject IProfileService ProfileService

<EditForm Model="Input" method="post" OnValidSubmit="SaveInterests" FormName="interests">
    <h1>Næsten færdig! Step 2 of 2</h1>

    @* Category selector - may need SSR-compatible version *@
    @* Or use checkboxes for categories *@
    <div class="category-grid">
        @foreach (var category in Categories)
        {
            <label>
                <input type="checkbox"
                       name="Input.SelectedCategories"
                       value="@category.Name"
                       checked="@Input.SelectedCategories.Contains(category.Name)" />
                @category.Name
            </label>
        }
    </div>

    <div class="form-floating">
        <InputTextArea @bind-Value="Input.Description"
                       class="form-control"
                       rows="5" />
        <label>Fortæl lidt om dig selv</label>
    </div>

    <button type="submit" class="btn btn-success">Fuldfør profil</button>
    <a href="/Account/CompleteProfile" class="btn btn-link">Tilbage</a>
    <a href="/" class="btn btn-link">Spring over</a>
</EditForm>

@code {
    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    private List<Category> Categories { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        Categories = await CategoryCache.GetOrCreateCategoriesAsync();
    }

    private async Task SaveInterests()
    {
        // Save categories and description
        await ProfileService.UpdateInterestsAsync(Input.SelectedCategories, Input.Description);

        // Redirect to home - profile complete!
        RedirectManager.RedirectTo("/");
    }

    private sealed class InputModel
    {
        public List<string> SelectedCategories { get; set; } = new();
        public string? Description { get; set; }
    }
}
```

## Handling Existing Interactive Components in SSR

### AddressAutoComplete / ZipCodeAutoComplete

These components likely use JavaScript interactivity for autocomplete. For SSR:

**Option 1:** Disable autocomplete, use simple text input
```razor
<div class="form-floating">
    <InputText @bind-Value="Input.Address" class="form-control" />
    <label>Adresse (f.eks. "Hovedgaden 1, 1000 København")</label>
</div>
```

**Option 2:** Use HTML5 datalist (SSR-compatible)
```razor
<input list="zipcodes"
       @bind="Input.ZipCode"
       class="form-control"
       placeholder="Postnummer" />
<datalist id="zipcodes">
    @foreach (var zip in PopularZipCodes)
    {
        <option value="@zip.Code">@zip.City</option>
    }
</datalist>
```

**Option 3:** Move to interactive page after email confirmation
- After email confirmation, redirect to `/profile` (interactive) instead of `/Account/CompleteProfile` (SSR)
- Use full interactive components there

### CategorySelector

Likely needs `@onclick` for selection. For SSR, use:
```razor
<div class="category-grid">
    @foreach (var category in Categories)
    {
        <label class="category-checkbox">
            <input type="checkbox"
                   name="SelectedCategories"
                   value="@category.Name" />
            <span>@category.Name</span>
        </label>
    }
</div>
```

### TextEditorComponent (WYSIWYG)

Rich text editors require JavaScript. For SSR:
- Use simple `<textarea>` instead
- Or redirect to `/profile` page (interactive) for editing description

## Key Takeaways

1. **Two separate pages** for two-step flow (not in-page toggling)
2. **Use radio buttons** for location type selection (not toggle button)
3. **Form submissions** navigate between pages
4. **Simple inputs** instead of interactive components where needed
5. **Or redirect to `/profile`** (interactive) for complex editing

## Alternative: Move Onboarding to Interactive Page

If SSR constraints are too limiting, consider:
- Keep `/Account/Register` and `/Account/ConfirmEmail` as SSR
- After email confirmation, redirect to `/profile/onboarding` (new interactive page)
- Use full Blazor Server interactivity there
- Allows use of AddressAutoComplete, CategorySelector, TextEditorComponent, etc.

**Trade-off:**
- ✅ Pro: Full interactivity, better UX
- ❌ Con: Different page location, not in `/Account` flow

## Reference Implementation

See these existing SSR pages for patterns:
- [Register.razor](src/web/Jordnaer/Components/Account/Pages/Register.razor) - Form submission, validation
- [Login.razor](src/web/Jordnaer/Components/Account/Pages/Login.razor) - Simple SSR form
- Any page in `/Account/Manage` - SSR patterns

## Summary

**The golden rule: If it requires `@onclick` or client-side state changes, it won't work in `/Account` pages.**

Use:
- Form submissions for all actions
- Radio buttons for choices
- Page redirects for navigation
- Simple inputs instead of interactive components
- Or move to `/profile/onboarding` for full interactivity

## External Authentication Integration

External login users (Google, Facebook, Microsoft) also need profile completion. The external login callback at [ExternalLogin.razor](src/web/Jordnaer/Components/Account/Pages/ExternalLogin.razor) is also an SSR page.

### Redirect After External Login

**For new external users** (line 189 in ExternalLogin.razor):
```csharp
// After creating user via external login
await SignInManager.SignInAsync(user, isPersistent: false, _externalLoginInfo.LoginProvider);

// Redirect to profile completion
RedirectManager.RedirectTo("/Account/CompleteProfile");
```

### Pre-Populated Fields from External Providers

External providers (Google, Facebook, Microsoft) send claims that populate:
- FirstName (from ClaimTypes.GivenName or ClaimTypes.Name)
- LastName (from ClaimTypes.Surname or ClaimTypes.Name)

**In CompleteProfile.razor**, check if these are already populated:

```csharp
protected override async Task OnInitializedAsync()
{
    var userProfile = await ProfileCache.GetProfileAsync();
    
    if (userProfile is null)
    {
        // Error handling
        return;
    }

    // Pre-populate from existing profile data (from external claims)
    Input.FirstName = userProfile.FirstName ?? "";
    Input.LastName = userProfile.LastName ?? "";
    
    // Username will be generated from these
    if (!string.IsNullOrWhiteSpace(Input.FirstName) && !string.IsNullOrWhiteSpace(Input.LastName))
    {
        _generatedUsername = await ProfileService.GenerateUniqueUsernameAsync(
            Input.FirstName,
            Input.LastName);
    }
}
```

**Show pre-populated fields as editable** (in case external provider data is wrong):
```razor
<div class="form-floating">
    <InputText @bind-Value="Input.FirstName" 
               class="form-control" 
               id="firstname" />
    <label for="firstname">Fornavn @(string.IsNullOrWhiteSpace(Input.FirstName) ? "*" : "(fra " + ProviderName + ")")</label>
    <ValidationMessage For="() => Input.FirstName" />
</div>
```

This way:
- External users see their name pre-filled
- They can edit if wrong
- They only need to add location, categories, etc.

