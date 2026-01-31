# Convert Existing User to Partner

## Summary
We need the ability to convert existing users to partners without requiring them to create a new account.

## Background
Currently, partners are created via the backoffice by creating a brand new account with a temporary password. This doesn't work well when an existing user wants to become a partner - they would need to abandon their existing account with its profile, contacts, group memberships, and chat history.

## Requirements

### Core Functionality
- Admin should be able to select an existing user and convert them to a partner
- The user keeps their existing `ApplicationUser` and `UserProfile` data
- A new `Partner` record is created and linked to their existing account
- The `Partner` role is assigned to the user

### Admin UI (Backoffice)
- Add a new page or extend existing partner management: `/backoffice/partners/convert`
- Search/select an existing user by email or username
- Validate the user is not already a partner
- Form to enter initial partner details:
  - Partner Name (can default to user's display name)
  - Website Link (optional initially)
  - Description (optional)
  - Logo URL (optional)
  - Permissions: `CanHavePartnerCard`, `CanHaveAd`
- Confirmation before conversion
- Success message with link to partner details page

### Validation Rules
- User must exist and have a confirmed email
- User must NOT already have a `Partner` record
- Partner Name should follow existing validation (2-128 chars if provided)
- Website Link should be valid URL format if provided

### Data Model (No Changes Required)
The existing data model already supports this:
- `Partner.UserId` links to `ApplicationUser.Id`
- `UserProfile` is independent of partner status
- Role system already has `Partner` role

## Technical Implementation

### Service Layer
Create conversion logic in `PartnerUserService` or new service:
```csharp
Task<Result<Partner>> ConvertUserToPartnerAsync(
    Guid userId,
    string? partnerName,
    string? link,
    string? description,
    bool canHavePartnerCard,
    bool canHaveAd);
```

### Steps
1. Validate user exists and is not already a partner
2. Begin transaction
3. Create `Partner` record with provided details
4. Assign `Partner` role via `IUserRoleService`
5. Commit transaction
6. Optionally send notification email to user about their new partner status

### Error Handling
- User not found → appropriate error message
- User already a partner → show existing partner details
- Transaction failure → rollback all changes

## Related Files
- [PartnerUserService.cs](src/web/Jordnaer/Features/Partners/PartnerUserService.cs) - existing partner creation logic
- [CreatePartnerPage.razor](src/web/Jordnaer/Pages/Backoffice/CreatePartnerPage.razor) - UI patterns
- [UserClaimManagementPage.razor](src/web/Jordnaer/Pages/Backoffice/UserClaimManagementPage.razor) - user search patterns
- [Partner.cs](src/shared/Jordnaer.Shared/Database/Partner.cs) - entity model
- [UserRoleService.cs](src/web/Jordnaer/Features/Authentication/UserRoleService.cs) - role management

## Out of Scope
- Self-service partner registration (user requesting to become partner)
- Migrating data from UserProfile to Partner (they serve different purposes)
- Revoking partner status / converting partner back to regular user (separate task if needed)
