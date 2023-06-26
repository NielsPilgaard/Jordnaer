IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    CREATE TABLE [LookingFor] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [CreatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_LookingFor] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    CREATE TABLE [UserProfiles] (
        [Id] nvarchar(450) NOT NULL,
        [FirstName] nvarchar(100) NULL,
        [LastName] nvarchar(250) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [Address] nvarchar(500) NULL,
        [ZipCode] nvarchar(50) NULL,
        [City] nvarchar(100) NULL,
        [Description] nvarchar(2000) NULL,
        [DateOfBirth] datetime2 NULL,
        [ProfilePictureUrl] nvarchar(max) NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [UserProfileId] nvarchar(450) NULL,
        CONSTRAINT [PK_UserProfiles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserProfiles_UserProfiles_UserProfileId] FOREIGN KEY ([UserProfileId]) REFERENCES [UserProfiles] ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    CREATE TABLE [ChildProfiles] (
        [Id] uniqueidentifier NOT NULL,
        [UserProfileId] nvarchar(450) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(250) NULL,
        [Gender] int NOT NULL,
        [DateOfBirth] datetime2 NULL,
        [Description] nvarchar(2000) NULL,
        [PictureUrl] nvarchar(max) NULL,
        [CreatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ChildProfiles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChildProfiles_UserProfiles_UserProfileId] FOREIGN KEY ([UserProfileId]) REFERENCES [UserProfiles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    CREATE TABLE [UserProfileLookingFor] (
        [UserProfileId] nvarchar(450) NOT NULL,
        [LookingForId] int NOT NULL,
        CONSTRAINT [PK_UserProfileLookingFor] PRIMARY KEY ([LookingForId], [UserProfileId]),
        CONSTRAINT [FK_UserProfileLookingFor_LookingFor_LookingForId] FOREIGN KEY ([LookingForId]) REFERENCES [LookingFor] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserProfileLookingFor_UserProfiles_UserProfileId] FOREIGN KEY ([UserProfileId]) REFERENCES [UserProfiles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    CREATE INDEX [IX_ChildProfiles_UserProfileId] ON [ChildProfiles] ([UserProfileId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    CREATE INDEX [IX_UserProfileLookingFor_UserProfileId] ON [UserProfileLookingFor] ([UserProfileId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    CREATE INDEX [IX_UserProfiles_FirstName_LastName] ON [UserProfiles] ([FirstName], [LastName]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    CREATE INDEX [IX_UserProfiles_UserProfileId] ON [UserProfiles] ([UserProfileId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    CREATE INDEX [IX_UserProfiles_ZipCode_City] ON [UserProfiles] ([ZipCode], [City]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230430201410_Initial')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230430201410_Initial', N'7.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230505180341_UserContacts')
BEGIN
    ALTER TABLE [UserProfiles] DROP CONSTRAINT [FK_UserProfiles_UserProfiles_UserProfileId];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230505180341_UserContacts')
BEGIN
    DROP INDEX [IX_UserProfiles_UserProfileId] ON [UserProfiles];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230505180341_UserContacts')
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserProfiles]') AND [c].[name] = N'UserProfileId');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [UserProfiles] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [UserProfiles] DROP COLUMN [UserProfileId];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230505180341_UserContacts')
BEGIN
    CREATE TABLE [UserContacts] (
        [UserProfileId] nvarchar(450) NOT NULL,
        [ContactId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_UserContacts] PRIMARY KEY ([ContactId], [UserProfileId]),
        CONSTRAINT [FK_UserContacts_UserProfiles_ContactId] FOREIGN KEY ([ContactId]) REFERENCES [UserProfiles] ([Id]),
        CONSTRAINT [FK_UserContacts_UserProfiles_UserProfileId] FOREIGN KEY ([UserProfileId]) REFERENCES [UserProfiles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230505180341_UserContacts')
BEGIN
    CREATE INDEX [IX_ChildProfiles_DateOfBirth] ON [ChildProfiles] ([DateOfBirth]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230505180341_UserContacts')
BEGIN
    CREATE INDEX [IX_ChildProfiles_Gender] ON [ChildProfiles] ([Gender]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230505180341_UserContacts')
BEGIN
    CREATE INDEX [IX_UserContacts_UserProfileId] ON [UserContacts] ([UserProfileId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230505180341_UserContacts')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230505180341_UserContacts', N'7.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230505181748_DefaultProfilePicture')
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserProfiles]') AND [c].[name] = N'ProfilePictureUrl');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [UserProfiles] DROP CONSTRAINT [' + @var1 + '];');
    EXEC(N'UPDATE [UserProfiles] SET [ProfilePictureUrl] = N'''' WHERE [ProfilePictureUrl] IS NULL');
    ALTER TABLE [UserProfiles] ALTER COLUMN [ProfilePictureUrl] nvarchar(max) NOT NULL;
    ALTER TABLE [UserProfiles] ADD DEFAULT N'' FOR [ProfilePictureUrl];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230505181748_DefaultProfilePicture')
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ChildProfiles]') AND [c].[name] = N'PictureUrl');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [ChildProfiles] DROP CONSTRAINT [' + @var2 + '];');
    EXEC(N'UPDATE [ChildProfiles] SET [PictureUrl] = N'''' WHERE [PictureUrl] IS NULL');
    ALTER TABLE [ChildProfiles] ALTER COLUMN [PictureUrl] nvarchar(max) NOT NULL;
    ALTER TABLE [ChildProfiles] ADD DEFAULT N'' FOR [PictureUrl];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230505181748_DefaultProfilePicture')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230505181748_DefaultProfilePicture', N'7.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230505190358_AddUserName')
BEGIN
    DROP INDEX [IX_UserProfiles_FirstName_LastName] ON [UserProfiles];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230505190358_AddUserName')
BEGIN
    ALTER TABLE [UserProfiles] ADD [UserName] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230505190358_AddUserName')
BEGIN
    CREATE INDEX [IX_UserProfiles_UserName] ON [UserProfiles] ([UserName]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230505190358_AddUserName')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230505190358_AddUserName', N'7.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230514200453_AddSearchableName')
BEGIN
    DROP INDEX [IX_UserProfiles_ZipCode_City] ON [UserProfiles];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230514200453_AddSearchableName')
BEGIN
    EXEC(N'ALTER TABLE [UserProfiles] ADD [SearchableName] AS [FirstName] + [LastName] + [UserName] PERSISTED');
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230514200453_AddSearchableName')
BEGIN
    CREATE INDEX [IX_UserProfiles_SearchableName] ON [UserProfiles] ([SearchableName]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230514200453_AddSearchableName')
BEGIN
    CREATE INDEX [IX_UserProfiles_ZipCode] ON [UserProfiles] ([ZipCode]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230514200453_AddSearchableName')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230514200453_AddSearchableName', N'7.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230607211415_ConvertZipCodeToInt')
BEGIN
    DROP INDEX [IX_UserProfiles_ZipCode] ON [UserProfiles];
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserProfiles]') AND [c].[name] = N'ZipCode');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [UserProfiles] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [UserProfiles] ALTER COLUMN [ZipCode] int NULL;
    CREATE INDEX [IX_UserProfiles_ZipCode] ON [UserProfiles] ([ZipCode]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230607211415_ConvertZipCodeToInt')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230607211415_ConvertZipCodeToInt', N'7.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230626113357_Ages')
BEGIN
    EXEC(N'ALTER TABLE [UserProfiles] ADD [Age] AS DATEDIFF(YY, [DateOfBirth], GETDATE()) - CASE WHEN MONTH([DateOfBirth]) > MONTH(GETDATE()) OR MONTH([DateOfBirth]) = MONTH(GETDATE()) AND DAY([DateOfBirth]) > DAY(GETDATE()) THEN 1 ELSE 0 END');
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230626113357_Ages')
BEGIN
    EXEC(N'ALTER TABLE [ChildProfiles] ADD [Age] AS DATEDIFF(YY, [DateOfBirth], GETDATE()) - CASE WHEN MONTH([DateOfBirth]) > MONTH(GETDATE()) OR MONTH([DateOfBirth]) = MONTH(GETDATE()) AND DAY([DateOfBirth]) > DAY(GETDATE()) THEN 1 ELSE 0 END');
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230626113357_Ages')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230626113357_Ages', N'7.0.8');
END;
GO

COMMIT;
GO

