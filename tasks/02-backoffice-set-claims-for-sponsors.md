# Task 02: Backoffice Claims Management for Sponsors

## Context

**App:** Jordnaer (.NET Blazor Server)
**Area:** Authentication & Authorization
**Priority:** High (blocking for Task 01)
**Related:** Task 01 (Sponsor Dashboard) - sponsors need claims to access dashboard

## Objective

Create a backoffice admin interface for managing user claims, specifically to grant sponsor access permissions. This allows admins to designate which users can access sponsor dashboard features.

## Current State

- Authentication uses ASP.NET Core Identity with [ApplicationUser](src/web/Jordnaer/Database/ApplicationUser.cs)
- [CurrentUser.cs](src/web/Jordnaer/Features/Authentication/CurrentUser.cs) provides user context via ClaimsPrincipal
- Claims infrastructure exists via [Claim.cs](src/shared/Jordnaer.Shared/Auth/Claim.cs)
- Group membership uses [PermissionLevel](src/shared/Jordnaer.Shared/Database/Enums/PermissionLevel.cs) and [OwnershipLevel](src/shared/Jordnaer.Shared/Database/Enums/OwnershipLevel.cs) enums
- Existing admin pattern in [Members.razor](src/web/Jordnaer/Pages/Groups/Members.razor) for managing group permissions
- **No backoffice UI exists for managing user claims globally**

## Requirements

### 1. Backoffice Admin Page

- Create page at `/backoffice/users` route
- Require `[Authorize]` with admin-only access
- Use MudBlazor DataGrid component with proper edit UX (NOT the janky edit-on-click from current Members.razor)
- Display all users with their current claims
- Support searching/filtering by username or email

### 2. Claims Management UI

**Display columns:**
- Username
- Email (GDPR-masked display)
- Current claims (comma-separated or chip display)
- Actions (Edit button)

**Edit functionality:**
- Edit button in actions column (opens dialog)
- Dialog with multi-select for available claims
- Save/Cancel actions in dialog
- Optimistic UI updates
- Error handling with user feedback
- Clear visual feedback for changes

### 3. Sponsor Claim Type

Define new claim type for sponsors:
```csharp
public static class ClaimTypes
{
    public const string Sponsor = "sponsor";
    public const string Admin = "admin";
}
```

When user has `sponsor` claim:
- Can access `/sponsor/dashboard` (Task 01)
- Can manage their sponsor's ad images
- Can view analytics for their sponsor account

### 4. Admin Authorization

**Admin check pattern:**
```csharp
// Check if current user has admin claim
private bool _isAdmin;

protected override async Task OnInitializedAsync()
{
    _isAdmin = CurrentUser.User?.HasClaim(c => c.Type == ClaimTypes.Admin) ?? false;

    if (!_isAdmin)
    {
        Navigation.NavigateTo("/");
    }
}
```

**Authorize attribute approach (preferred):**
```csharp
@attribute [Authorize(Policy = "AdminOnly")]
```

Register policy in Program.cs:
```csharp
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim(ClaimTypes.Admin));
    options.AddPolicy("SponsorAccess", policy =>
        policy.RequireClaim(ClaimTypes.Sponsor));
});
```

### 5. User-Claim Management Service

Create service for managing user claims:

```csharp
public interface IUserClaimService
{
    Task<List<UserClaimDto>> GetAllUsersWithClaimsAsync();
    Task<OneOf<Success, NotFound>> AddClaimToUserAsync(string userId, string claimType, string claimValue);
    Task<OneOf<Success, NotFound>> RemoveClaimFromUserAsync(string userId, string claimType, string claimValue);
    Task<List<System.Security.Claims.Claim>> GetUserClaimsAsync(string userId);
}

public class UserClaimDto
{
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public List<ClaimDto> Claims { get; set; }
}

public class ClaimDto
{
    public string Type { get; set; }
    public string Value { get; set; }
}
```

### 6. Database Integration

Uses ASP.NET Core Identity tables (already exist):
- `AspNetUserClaims` - Stores user claims
- `AspNetUsers` - User accounts

Methods to implement:
```csharp
// Via UserManager<ApplicationUser>
await userManager.AddClaimAsync(user, new Claim(claimType, claimValue));
await userManager.RemoveClaimAsync(user, new Claim(claimType, claimValue));
var claims = await userManager.GetClaimsAsync(user);
```

### 7. Audit Logging

- Log all claim additions/removals
- Include: admin user ID, target user ID, claim type, timestamp
- Use existing logging infrastructure
- GDPR-compliant: mask email addresses in logs

Example:
```csharp
logger.LogInformation(
    "Admin {AdminId} added claim {ClaimType} to user {UserId} at {Timestamp}",
    currentUserId,
    claimType,
    targetUserId,
    DateTime.UtcNow
);
```

## Acceptance Criteria

### Backoffice Page
- [ ] New page at `/backoffice/users` with admin authorization
- [ ] Only accessible to users with admin claim
- [ ] MudBlazor DataGrid displays all users
- [ ] Search/filter by username or email
- [ ] GDPR-compliant email masking

### Claims Management
- [ ] Edit button opens modal dialog
- [ ] Dialog has multi-select for available claims
- [ ] Dialog shows current claims pre-selected
- [ ] Save/Cancel actions work correctly
- [ ] Optimistic UI updates
- [ ] Error handling with user feedback
- [ ] Can add sponsor claim to users
- [ ] Can remove sponsor claim from users

### Authorization
- [ ] `AdminOnly` policy registered in Program.cs
- [ ] `SponsorAccess` policy registered in Program.cs
- [ ] Admin check prevents unauthorized access
- [ ] Sponsor claim grants access to sponsor dashboard (Task 01)

### Service Implementation
- [ ] UserClaimService created and registered in DI
- [ ] GetAllUsersWithClaimsAsync implemented
- [ ] AddClaimToUserAsync implemented
- [ ] RemoveClaimFromUserAsync implemented
- [ ] Uses UserManager<ApplicationUser> correctly
- [ ] OneOf pattern for error handling

### Audit & Security
- [ ] All claim changes are logged
- [ ] Logs include admin ID, target user ID, claim type, timestamp
- [ ] Email addresses are GDPR-masked in logs
- [ ] Cannot edit own claims (prevent self-lockout)
- [ ] Validation prevents invalid claim types

## Files to Create/Modify

**New Files:**
- `src/web/Jordnaer/Pages/Backoffice/Users.razor` - Admin page for user management
- `src/web/Jordnaer/Features/Authentication/UserClaimService.cs` - Service for claim management
- `src/web/Jordnaer/Features/Authentication/ClaimTypes.cs` - Claim type constants

**Modify:**
- `src/web/Jordnaer/Program.cs` - Register authorization policies and UserClaimService
- [CurrentUser.cs](src/web/Jordnaer/Features/Authentication/CurrentUser.cs) - Add helper methods for claim checks (optional)

## Technical Notes

- Use MudDialog for editing claims (better UX than inline editing)
- Use `UserManager<ApplicationUser>` for claim operations (injected via DI)
- ASP.NET Core Identity handles claim persistence automatically
- Claims are stored in `AspNetUserClaims` table (no migration needed)
- After adding/removing claims, user must re-login for changes to take effect
- Consider adding a "Force re-login" notification to users
- Use MudBlazor's MudChip for displaying claims (visual clarity)
- Implement pagination for user list if user count grows large

## Security Considerations

- **Critical:** Prevent admins from removing their own admin claim
- Validate claim types against whitelist (no arbitrary claim types)
- Log all claim modifications for audit trail
- Consider implementing "super admin" role that cannot be revoked
- GDPR: Only display necessary user information
- Rate limiting on claim modification endpoints (prevent abuse)
