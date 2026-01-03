# Admin Role Setup

## Adding Admin Role to a User

This document describes how to manually assign the Admin role to a user in the database.

### Prerequisites

- Access to the SQL Server database
- The user's email address

### SQL Script

Run the following SQL script to assign the Admin role to a user:

```sql
-- Step 1: First, ensure the "Admin" role exists in AspNetRoles
-- (Check if it exists, if not create it)
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Admin')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'Admin', 'ADMIN', NEWID())
END

-- Step 2: Add the Admin role to a specific user
-- Replace 'user@example.com' with the actual user's email
DECLARE @UserId NVARCHAR(450)
DECLARE @RoleId NVARCHAR(450)

-- Get the user's ID by email
SELECT @UserId = Id FROM AspNetUsers WHERE Email = 'user@example.com'

-- Get the Admin role ID
SELECT @RoleId = Id FROM AspNetRoles WHERE Name = 'Admin'

-- Insert the user-role relationship if it doesn't already exist
IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @UserId AND RoleId = @RoleId)
BEGIN
    INSERT INTO AspNetUserRoles (UserId, RoleId)
    VALUES (@UserId, @RoleId)
END
```

### Instructions

1. Replace `'user@example.com'` with the actual email address of the user you want to make an admin
2. Execute the script against your database
3. The script will:
   - Create the Admin role if it doesn't exist
   - Find the user by email
   - Assign the Admin role to the user if not already assigned

### Verification

To verify the role was assigned correctly, run:

```sql
SELECT u.Email, r.Name
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'user@example.com'
```

This should return a row showing the user's email and their 'Admin' role.
