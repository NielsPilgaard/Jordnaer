# Better Group Invite Over Email Flow

## Goal

Enable group admins to invite users who are **not yet registered** on the platform. The invited user must have a seamless experience from email → registration → profile completion → email confirmation → landing on the group page.

## Current State

The current invitation system **only supports existing users**:

- `InviteUserToGroupAsync()` requires an existing `userId`
- `SendGroupInviteEmail()` queries for an existing user's email
- No mechanism exists to invite by email address for unregistered users
- No invite tokens or special registration links

### Key Files (Current Implementation)

| File | Purpose |
|------|---------|
| [MembershipService.cs](src/web/Jordnaer/Features/Membership/MembershipService.cs) | Core invitation logic |
| [EmailService.cs](src/web/Jordnaer/Features/Email/EmailService.cs) | Email sending |
| [InviteUserDialog.razor](src/web/Jordnaer/Features/Groups/Components/InviteUserDialog.razor) | UI for inviting users |
| [Register.razor](src/web/Jordnaer/Components/Account/Pages/Register.razor) | User registration |
| [CompleteProfile.razor](src/web/Jordnaer/Pages/Profile/CompleteProfile.razor) | Profile step 1 |
| [CompleteProfileInterests.razor](src/web/Jordnaer/Pages/Profile/CompleteProfileInterests.razor) | Profile step 2 |
| [ConfirmEmail.razor](src/web/Jordnaer/Components/Account/Pages/ConfirmEmail.razor) | Email confirmation |

## Required Changes

### 1. Database: New `PendingGroupInvite` Table

Create a new entity to track email-based invitations:

```csharp
public class PendingGroupInvite
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Group Group { get; set; }
    public string Email { get; set; }              // Invited email address
    public string Token { get; set; }              // Hashed unique token
    public PendingInviteStatus Status { get; set; } // Pending, Accepted, Expired
    public DateTime CreatedUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }
    public string? InvitedByUserId { get; set; }   // Who sent the invite
}

public enum PendingInviteStatus
{
    Pending,
    Accepted,
    Expired
}
```

### 2. InviteUserDialog: Support Email Input

Modify [InviteUserDialog.razor](src/web/Jordnaer/Features/Groups/Components/InviteUserDialog.razor) to:

- Add option to invite by email address (not just existing user search)
- Validate email format
- Call new `InviteByEmailAsync` method

### 3. MembershipService: New Invite Method

Add to [MembershipService.cs](src/web/Jordnaer/Features/Membership/MembershipService.cs):

```csharp
public async Task<Result> InviteByEmailAsync(Guid groupId, string email, string invitedByUserId)
{
    // 1. Check if email is already registered → use existing flow
    // 2. Check if pending invite already exists for this email/group
    // 3. Generate secure token
    // 4. Create PendingGroupInvite record
    // 5. Send email with invite link
}
```

### 4. EmailService: New Email Template

Add to [EmailService.cs](src/web/Jordnaer/Features/Email/EmailService.cs):

- New method `SendGroupInviteEmailToNewUserAsync(email, groupName, token)`
- Email contains link: `/Account/Register?inviteToken={token}`
- Include group name and inviter name in email body

### 5. Registration Flow: Accept Invite Token

Modify [Register.razor](src/web/Jordnaer/Components/Account/Pages/Register.razor):

- Accept `inviteToken` query parameter
- Validate token and extract group info
- Pre-fill email field from token (readonly)
- Pass token through to next steps via `returnUrl`

### 6. Profile Completion: Maintain Token

Modify [CompleteProfile.razor](src/web/Jordnaer/Pages/Profile/CompleteProfile.razor) and [CompleteProfileInterests.razor](src/web/Jordnaer/Pages/Profile/CompleteProfileInterests.razor):

- Accept and preserve `inviteToken` in query string
- Pass through to final redirect

### 7. Email Confirmation: Complete Invite

Modify [ConfirmEmail.razor](src/web/Jordnaer/Components/Account/Pages/ConfirmEmail.razor):

- After email confirmation, check for valid `inviteToken`
- If valid: auto-accept group membership, mark invite as accepted
- Redirect to group page

## User Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ 1. Admin invites user@example.com to "Fodboldklubben"                       │
└─────────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│ 2. PendingGroupInvite created with token, email sent                        │
│    Link: /Account/Register?inviteToken=abc123                               │
└─────────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│ 3. User clicks link → Register page                                         │
│    - Email pre-filled (readonly)                                            │
│    - User creates password                                                  │
│    - returnUrl set to /groups/fodboldklubben?inviteToken=abc123             │
└─────────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│ 4. CompleteProfile → CompleteProfileInterests                               │
│    - inviteToken maintained in returnUrl throughout                         │
└─────────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│ 5. Email confirmation sent                                                  │
│    Link includes ReturnUrl=/groups/fodboldklubben?inviteToken=abc123        │
└─────────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│ 6. User confirms email                                                      │
│    - Token validated                                                        │
│    - GroupMembership created with Status=Active                             │
│    - PendingGroupInvite marked as Accepted                                  │
│    - Redirect to /groups/fodboldklubben                                     │
└─────────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│ 7. User lands on group page as a member ✓                                   │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Edge Cases to Handle

1. **Email already registered**: Show message "This email is already registered. Please log in to accept the invite."
2. **Token expired**: Show friendly message with option to request new invite
3. **Token already used**: Redirect to group if user is member, otherwise show error
4. **User abandons registration**: Token remains valid until expiration
5. **Admin resends invite**: Invalidate old token, create new one

## Security Considerations

- Tokens should be cryptographically secure (use `RandomNumberGenerator`)
- Store hashed tokens in database, send unhashed in email
- Tokens expire after reasonable time (e.g., 7 days)
- Rate limit invite sending to prevent abuse
- Validate email format on frontend and backend

## Migration Notes

- New `PendingGroupInvite` table via EF Core migration
- No impact on existing invites (they target registered users)
- Both flows (existing user / new user) should coexist
