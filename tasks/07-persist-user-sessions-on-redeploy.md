# Task 07: Persist User Sessions on Redeploy

## Context

**App:** Jordnaer (.NET Blazor Server)
**Area:** Authentication / Infrastructure
**Priority:** High
**Related:** None

## Objective

Ensure users remain logged in when the website is redeployed. Currently, all users are logged out during deployments because authentication data protection keys are not persisted.

## Current State

- When the application restarts (redeploy, container restart), all user sessions are invalidated
- Users must log in again after each deployment
- Data protection keys are likely stored in memory or temporary storage
- This creates a poor user experience, especially for frequent deployments

## Root Cause

ASP.NET Core uses Data Protection for encrypting authentication cookies. By default, keys are:
- Generated on app startup
- Stored in memory or ephemeral storage
- Lost when the application restarts

When keys are lost, existing authentication cookies become unreadable, forcing users to re-authenticate.

## Requirements

### 1. Persist Data Protection Keys

Configure Data Protection to persist keys to a durable store:

**Azure Blob Storage - Recommended**

Use existing Azure Storage account to persist keys:
```csharp
services.AddDataProtection()
    .SetApplicationName("Jordnaer")
    .PersistKeysToAzureBlobStorage(blobUri);
```

Or with connection string:
```csharp
var blobClient = new BlobClient(connectionString, "data-protection", "keys.xml");
services.AddDataProtection()
    .SetApplicationName("Jordnaer")
    .PersistKeysToAzureBlobStorage(blobClient);
```

## Implementation Steps

1. Add NuGet package: `Azure.Extensions.AspNetCore.DataProtection.Blobs`
2. Create a `data-protection` container in Azure Storage (or use existing container)
3. Configure Data Protection in `Program.cs` using existing storage connection string
4. Deploy and verify sessions survive restarts

## Acceptance Criteria

- [ ] Data protection keys are persisted to Azure Blob Storage
- [ ] Users remain logged in after application restarts
- [ ] Users remain logged in after deployments
- [ ] Configuration uses environment-specific settings (dev vs prod)

## Files to Modify

- `src/web/Jordnaer/Program.cs` - Add Data Protection configuration

## Technical Notes

- If using multiple instances/replicas, all instances must share the same key storage
- Key rotation is handled automatically by ASP.NET Core
- Existing users will still be logged out on first deployment after this change (one-time event)
- Consider key lifetime settings for security compliance
- Test with `dotnet watch` restarts during development to verify persistence

## References

- [ASP.NET Core Data Protection](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/introduction)
- [Configure Data Protection](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview)
- [Data Protection key management](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-management)
