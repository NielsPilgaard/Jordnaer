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
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [CreatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE TABLE [Chats] (
        [Id] uniqueidentifier NOT NULL,
        [DisplayName] nvarchar(max) NULL,
        [LastMessageSentUtc] datetime2 NOT NULL,
        [StartedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Chats] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE TABLE [Groups] (
        [Id] uniqueidentifier NOT NULL,
        [ProfilePictureUrl] nvarchar(max) NULL,
        [Address] nvarchar(500) NULL,
        [ZipCode] int NULL,
        [City] nvarchar(100) NULL,
        [Name] nvarchar(128) NOT NULL,
        [ShortDescription] nvarchar(200) NOT NULL,
        [Description] nvarchar(4000) NULL,
        [CreatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Groups] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE TABLE [UnreadMessages] (
        [Id] bigint NOT NULL IDENTITY,
        [ChatId] uniqueidentifier NOT NULL,
        [RecipientId] nvarchar(max) NOT NULL,
        [MessageSentUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_UnreadMessages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE TABLE [UserProfiles] (
        [Id] nvarchar(450) NOT NULL,
        [UserName] nvarchar(100) NULL,
        [FirstName] nvarchar(100) NULL,
        [LastName] nvarchar(250) NULL,
        [SearchableName] AS ISNULL([FirstName], '') + ' ' + ISNULL([LastName], '') + ' ' + ISNULL([UserName], '') PERSISTED,
        [PhoneNumber] nvarchar(max) NULL,
        [Address] nvarchar(500) NULL,
        [ZipCode] int NULL,
        [City] nvarchar(100) NULL,
        [Description] nvarchar(2000) NULL,
        [DateOfBirth] datetime2 NULL,
        [ProfilePictureUrl] nvarchar(max) NOT NULL,
        [Age] AS DATEDIFF(YY, [DateOfBirth], GETDATE()) - CASE WHEN MONTH([DateOfBirth]) > MONTH(GETDATE()) OR MONTH([DateOfBirth]) = MONTH(GETDATE()) AND DAY([DateOfBirth]) > DAY(GETDATE()) THEN 1 ELSE 0 END,
        [CreatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_UserProfiles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE TABLE [GroupCategories] (
        [GroupId] uniqueidentifier NOT NULL,
        [CategoryId] int NOT NULL,
        CONSTRAINT [PK_GroupCategories] PRIMARY KEY ([CategoryId], [GroupId]),
        CONSTRAINT [FK_GroupCategories_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_GroupCategories_Groups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [Groups] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE TABLE [ChatMessages] (
        [Id] uniqueidentifier NOT NULL,
        [SenderId] nvarchar(450) NOT NULL,
        [ChatId] uniqueidentifier NOT NULL,
        [Text] nvarchar(max) NOT NULL,
        [SentUtc] datetime2 NOT NULL,
        [AttachmentUrl] nvarchar(max) NULL,
        CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChatMessages_Chats_ChatId] FOREIGN KEY ([ChatId]) REFERENCES [Chats] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ChatMessages_UserProfiles_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [UserProfiles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE TABLE [ChildProfiles] (
        [Id] uniqueidentifier NOT NULL,
        [UserProfileId] nvarchar(450) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(250) NULL,
        [Gender] int NOT NULL,
        [DateOfBirth] datetime2 NULL,
        [Description] nvarchar(2000) NULL,
        [PictureUrl] nvarchar(max) NOT NULL,
        [Age] AS DATEDIFF(YY, [DateOfBirth], GETDATE()) - CASE WHEN MONTH([DateOfBirth]) > MONTH(GETDATE()) OR MONTH([DateOfBirth]) = MONTH(GETDATE()) AND DAY([DateOfBirth]) > DAY(GETDATE()) THEN 1 ELSE 0 END,
        [CreatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ChildProfiles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChildProfiles_UserProfiles_UserProfileId] FOREIGN KEY ([UserProfileId]) REFERENCES [UserProfiles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE TABLE [GroupMemberships] (
        [GroupId] uniqueidentifier NOT NULL,
        [UserProfileId] nvarchar(450) NOT NULL,
        [UserInitiatedMembership] bit NOT NULL,
        [CreatedUtc] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [LastUpdatedUtc] datetime2 NOT NULL,
        [MembershipStatus] int NOT NULL,
        [PermissionLevel] int NOT NULL,
        [OwnershipLevel] int NOT NULL,
        CONSTRAINT [PK_GroupMemberships] PRIMARY KEY ([GroupId], [UserProfileId]),
        CONSTRAINT [FK_GroupMemberships_Groups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [Groups] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_GroupMemberships_UserProfiles_UserProfileId] FOREIGN KEY ([UserProfileId]) REFERENCES [UserProfiles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE TABLE [UserChats] (
        [UserProfileId] nvarchar(450) NOT NULL,
        [ChatId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UserChats] PRIMARY KEY ([ChatId], [UserProfileId]),
        CONSTRAINT [FK_UserChats_Chats_ChatId] FOREIGN KEY ([ChatId]) REFERENCES [Chats] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserChats_UserProfiles_UserProfileId] FOREIGN KEY ([UserProfileId]) REFERENCES [UserProfiles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE TABLE [UserContacts] (
        [UserProfileId] nvarchar(450) NOT NULL,
        [ContactId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_UserContacts] PRIMARY KEY ([ContactId], [UserProfileId]),
        CONSTRAINT [FK_UserContacts_UserProfiles_ContactId] FOREIGN KEY ([ContactId]) REFERENCES [UserProfiles] ([Id]),
        CONSTRAINT [FK_UserContacts_UserProfiles_UserProfileId] FOREIGN KEY ([UserProfileId]) REFERENCES [UserProfiles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE TABLE [UserProfileCategories] (
        [UserProfileId] nvarchar(450) NOT NULL,
        [CategoryId] int NOT NULL,
        CONSTRAINT [PK_UserProfileCategories] PRIMARY KEY ([CategoryId], [UserProfileId]),
        CONSTRAINT [FK_UserProfileCategories_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserProfileCategories_UserProfiles_UserProfileId] FOREIGN KEY ([UserProfileId]) REFERENCES [UserProfiles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_ChatId] ON [ChatMessages] ([ChatId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_SenderId] ON [ChatMessages] ([SenderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_SentUtc] ON [ChatMessages] ([SentUtc] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE INDEX [IX_Chats_LastMessageSentUtc] ON [Chats] ([LastMessageSentUtc] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE INDEX [IX_ChildProfiles_DateOfBirth] ON [ChildProfiles] ([DateOfBirth]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE INDEX [IX_ChildProfiles_Gender] ON [ChildProfiles] ([Gender]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE INDEX [IX_ChildProfiles_UserProfileId] ON [ChildProfiles] ([UserProfileId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE INDEX [IX_GroupCategories_GroupId] ON [GroupCategories] ([GroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE INDEX [IX_GroupMemberships_UserProfileId] ON [GroupMemberships] ([UserProfileId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE INDEX [IX_Groups_Name] ON [Groups] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE INDEX [IX_UserChats_UserProfileId] ON [UserChats] ([UserProfileId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE INDEX [IX_UserContacts_UserProfileId] ON [UserContacts] ([UserProfileId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE INDEX [IX_UserProfileCategories_UserProfileId] ON [UserProfileCategories] ([UserProfileId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE INDEX [IX_UserProfiles_SearchableName] ON [UserProfiles] ([SearchableName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE INDEX [IX_UserProfiles_UserName] ON [UserProfiles] ([UserName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    CREATE INDEX [IX_UserProfiles_ZipCode] ON [UserProfiles] ([ZipCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112182745_Initial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20231112182745_Initial', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112191227_Add_UniqueName_ToGroup'
)
BEGIN
    DROP INDEX [IX_Groups_Name] ON [Groups];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112191227_Add_UniqueName_ToGroup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Groups_Name] ON [Groups] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20231112191227_Add_UniqueName_ToGroup'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20231112191227_Add_UniqueName_ToGroup', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240219081648_Add_ApplicationUser_Cookie'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Cookie] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240219081648_Add_ApplicationUser_Cookie'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240219081648_Add_ApplicationUser_Cookie', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250220204935_Add_Posts_And_GroupPosts'
)
BEGIN
    CREATE TABLE [GroupPosts] (
        [Id] uniqueidentifier NOT NULL,
        [Text] nvarchar(1000) NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [ZipCode] int NULL,
        [UserProfileId] nvarchar(450) NOT NULL,
        [GroupId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_GroupPosts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GroupPosts_Groups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [Groups] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_GroupPosts_UserProfiles_UserProfileId] FOREIGN KEY ([UserProfileId]) REFERENCES [UserProfiles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250220204935_Add_Posts_And_GroupPosts'
)
BEGIN
    CREATE TABLE [Posts] (
        [Id] uniqueidentifier NOT NULL,
        [Text] nvarchar(1000) NOT NULL,
        [CreatedUtc] datetimeoffset NOT NULL,
        [ZipCode] int NULL,
        [City] nvarchar(max) NULL,
        [UserProfileId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_Posts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Posts_UserProfiles_UserProfileId] FOREIGN KEY ([UserProfileId]) REFERENCES [UserProfiles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250220204935_Add_Posts_And_GroupPosts'
)
BEGIN
    CREATE TABLE [PostCategory] (
        [PostId] uniqueidentifier NOT NULL,
        [CategoryId] int NOT NULL,
        CONSTRAINT [PK_PostCategory] PRIMARY KEY ([CategoryId], [PostId]),
        CONSTRAINT [FK_PostCategory_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PostCategory_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250220204935_Add_Posts_And_GroupPosts'
)
BEGIN
    CREATE INDEX [IX_GroupPosts_GroupId] ON [GroupPosts] ([GroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250220204935_Add_Posts_And_GroupPosts'
)
BEGIN
    CREATE INDEX [IX_GroupPosts_UserProfileId] ON [GroupPosts] ([UserProfileId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250220204935_Add_Posts_And_GroupPosts'
)
BEGIN
    CREATE INDEX [IX_PostCategory_PostId] ON [PostCategory] ([PostId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250220204935_Add_Posts_And_GroupPosts'
)
BEGIN
    CREATE INDEX [IX_Posts_UserProfileId] ON [Posts] ([UserProfileId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250220204935_Add_Posts_And_GroupPosts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250220204935_Add_Posts_And_GroupPosts', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250220210616_Add_ZipCodeIndex_On_Post'
)
BEGIN
    CREATE INDEX [IX_Posts_ZipCode] ON [Posts] ([ZipCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250220210616_Add_ZipCodeIndex_On_Post'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250220210616_Add_ZipCodeIndex_On_Post', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251218120748_Increase_Post_Text_Limit'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Posts]') AND [c].[name] = N'Text');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Posts] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Posts] ALTER COLUMN [Text] nvarchar(4000) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251218120748_Increase_Post_Text_Limit'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[GroupPosts]') AND [c].[name] = N'Text');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [GroupPosts] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [GroupPosts] ALTER COLUMN [Text] nvarchar(4000) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251218120748_Increase_Post_Text_Limit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251218120748_Increase_Post_Text_Limit', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251218130525_Remove_GroupPost_ZipCode'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[GroupPosts]') AND [c].[name] = N'ZipCode');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [GroupPosts] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [GroupPosts] DROP COLUMN [ZipCode];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251218130525_Remove_GroupPost_ZipCode'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251218130525_Remove_GroupPost_ZipCode', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222211350_Add_Location_Geography_Column'
)
BEGIN
    ALTER TABLE [UserProfiles] ADD [Location] geography NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222211350_Add_Location_Geography_Column'
)
BEGIN
    ALTER TABLE [Posts] ADD [Location] geography NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222211350_Add_Location_Geography_Column'
)
BEGIN
    ALTER TABLE [Groups] ADD [Location] geography NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222211350_Add_Location_Geography_Column'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251222211350_Add_Location_Geography_Column', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226115122_Unique_Username'
)
BEGIN
    DROP INDEX [IX_UserProfiles_UserName] ON [UserProfiles];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226115122_Unique_Username'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_UserProfiles_UserName] ON [UserProfiles] ([UserName]) WHERE [UserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226115122_Unique_Username'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251226115122_Unique_Username', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251229214906_Add_ZipCodeLocation_To_Groups'
)
BEGIN
    ALTER TABLE [Groups] ADD [ZipCodeLocation] geography NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251229214906_Add_ZipCodeLocation_To_Groups'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251229214906_Add_ZipCodeLocation_To_Groups', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260102194640_Add_NotificationSettings'
)
BEGIN
    ALTER TABLE [UserProfiles] ADD [ChatNotificationPreference] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260102194640_Add_NotificationSettings'
)
BEGIN
    ALTER TABLE [GroupMemberships] ADD [EmailOnNewPost] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260102194640_Add_NotificationSettings'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260102194640_Add_NotificationSettings', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260102225429_Fix_GroupMembership_Defaults'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[GroupMemberships]') AND [c].[name] = N'EmailOnNewPost');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [GroupMemberships] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [GroupMemberships] ADD DEFAULT CAST(1 AS bit) FOR [EmailOnNewPost];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260102225429_Fix_GroupMembership_Defaults'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260102225429_Fix_GroupMembership_Defaults', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260107220351_Add_Partners_And_Analytics'
)
BEGIN
    CREATE TABLE [Partners] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [LogoUrl] nvarchar(max) NULL,
        [Link] nvarchar(max) NOT NULL,
        [AdImageUrl] nvarchar(max) NULL,
        [PendingAdImageUrl] nvarchar(max) NULL,
        [PendingName] nvarchar(128) NULL,
        [PendingDescription] nvarchar(500) NULL,
        [PendingLogoUrl] nvarchar(max) NULL,
        [PendingLink] nvarchar(max) NULL,
        [LastUpdateUtc] datetime2 NULL,
        [HasPendingApproval] bit NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [CreatedUtc] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Partners] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Partners_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260107220351_Add_Partners_And_Analytics'
)
BEGIN
    CREATE TABLE [PartnerAnalytics] (
        [Id] int NOT NULL IDENTITY,
        [PartnerId] uniqueidentifier NOT NULL,
        [Date] datetime2 NOT NULL,
        [Impressions] int NOT NULL,
        [Clicks] int NOT NULL,
        CONSTRAINT [PK_PartnerAnalytics] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PartnerAnalytics_Partners_PartnerId] FOREIGN KEY ([PartnerId]) REFERENCES [Partners] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260107220351_Add_Partners_And_Analytics'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PartnerAnalytics_PartnerId_Date] ON [PartnerAnalytics] ([PartnerId], [Date]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260107220351_Add_Partners_And_Analytics'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Partners_Name] ON [Partners] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260107220351_Add_Partners_And_Analytics'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Partners_UserId] ON [Partners] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260107220351_Add_Partners_And_Analytics'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260107220351_Add_Partners_And_Analytics', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109222001_AddPartnerPermissions'
)
BEGIN
    DROP INDEX [IX_Partners_Name] ON [Partners];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109222001_AddPartnerPermissions'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Partners]') AND [c].[name] = N'Name');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Partners] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [Partners] ALTER COLUMN [Name] nvarchar(128) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109222001_AddPartnerPermissions'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Partners]') AND [c].[name] = N'Link');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Partners] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [Partners] ALTER COLUMN [Link] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109222001_AddPartnerPermissions'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Partners]') AND [c].[name] = N'Description');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Partners] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [Partners] ALTER COLUMN [Description] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109222001_AddPartnerPermissions'
)
BEGIN
    ALTER TABLE [Partners] ADD [CanHaveAd] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109222001_AddPartnerPermissions'
)
BEGIN
    ALTER TABLE [Partners] ADD [CanHavePartnerCard] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109222001_AddPartnerPermissions'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Partners_Name] ON [Partners] ([Name]) WHERE [Name] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109222001_AddPartnerPermissions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260109222001_AddPartnerPermissions', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109224836_Remove_Partner_Name_Index'
)
BEGIN
    DROP INDEX [IX_Partners_Name] ON [Partners];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109224836_Remove_Partner_Name_Index'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260109224836_Remove_Partner_Name_Index', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201210247_PendingGroupInvite_And_Partner_Optimizations'
)
BEGIN
    EXEC sp_rename N'[Partners].[PendingLink]', N'PendingPartnerPageLink', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201210247_PendingGroupInvite_And_Partner_Optimizations'
)
BEGIN
    EXEC sp_rename N'[Partners].[Link]', N'PendingAdLink', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201210247_PendingGroupInvite_And_Partner_Optimizations'
)
BEGIN
    ALTER TABLE [Partners] ADD [AdLabelColor] nvarchar(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201210247_PendingGroupInvite_And_Partner_Optimizations'
)
BEGIN
    ALTER TABLE [Partners] ADD [AdLink] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201210247_PendingGroupInvite_And_Partner_Optimizations'
)
BEGIN
    ALTER TABLE [Partners] ADD [PartnerPageLink] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201210247_PendingGroupInvite_And_Partner_Optimizations'
)
BEGIN
    ALTER TABLE [Partners] ADD [PendingAdLabelColor] nvarchar(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201210247_PendingGroupInvite_And_Partner_Optimizations'
)
BEGIN
    CREATE TABLE [PendingGroupInvites] (
        [Id] uniqueidentifier NOT NULL,
        [GroupId] uniqueidentifier NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [TokenHash] nvarchar(128) NOT NULL,
        [Status] int NOT NULL,
        [CreatedUtc] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [ExpiresAtUtc] datetime2 NOT NULL,
        [AcceptedAtUtc] datetime2 NULL,
        [InvitedByUserId] nvarchar(450) NULL,
        CONSTRAINT [PK_PendingGroupInvites] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PendingGroupInvites_Groups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [Groups] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PendingGroupInvites_UserProfiles_InvitedByUserId] FOREIGN KEY ([InvitedByUserId]) REFERENCES [UserProfiles] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201210247_PendingGroupInvite_And_Partner_Optimizations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PendingGroupInvites_Email_GroupId] ON [PendingGroupInvites] ([Email], [GroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201210247_PendingGroupInvite_And_Partner_Optimizations'
)
BEGIN
    CREATE INDEX [IX_PendingGroupInvites_GroupId] ON [PendingGroupInvites] ([GroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201210247_PendingGroupInvite_And_Partner_Optimizations'
)
BEGIN
    CREATE INDEX [IX_PendingGroupInvites_InvitedByUserId] ON [PendingGroupInvites] ([InvitedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201210247_PendingGroupInvite_And_Partner_Optimizations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PendingGroupInvites_TokenHash] ON [PendingGroupInvites] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201210247_PendingGroupInvite_And_Partner_Optimizations'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260201210247_PendingGroupInvite_And_Partner_Optimizations', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201221320_RenameChildProfilePictureUrl'
)
BEGIN
    EXEC sp_rename N'[ChildProfiles].[PictureUrl]', N'ProfilePictureUrl', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260201221320_RenameChildProfilePictureUrl'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260201221320_RenameChildProfilePictureUrl', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204154220_AddWebsiteUrlToGroups'
)
BEGIN
    ALTER TABLE [Groups] ADD [WebsiteUrl] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260204154220_AddWebsiteUrlToGroups'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260204154220_AddWebsiteUrlToGroups', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207075313_AddPartnerDisplayWindow'
)
BEGIN
    ALTER TABLE [Partners] ADD [DisplayEndUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207075313_AddPartnerDisplayWindow'
)
BEGIN
    ALTER TABLE [Partners] ADD [DisplayStartUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207075313_AddPartnerDisplayWindow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260207075313_AddPartnerDisplayWindow', N'10.0.2');
END;

COMMIT;
GO

