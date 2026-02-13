-- Data migration script: Convert UnreadMessages to Notifications
-- Run this AFTER the AddNotifications EF migration has been applied.
-- This script is idempotent - it checks for existing notifications before inserting.

SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY

    -- Create one notification per (RecipientId, ChatId) combination from UnreadMessages.
    -- Uses the most recent unread message timestamp for CreatedUtc.
    INSERT INTO [Notifications] ([Id], [RecipientId], [Title], [Description], [ImageUrl], [LinkUrl], [Type], [IsRead], [CreatedUtc], [ReadUtc], [SourceType], [SourceId])
    SELECT
        NEWID() AS [Id],
        um.[RecipientId],
        N'Du har ulæste beskeder' AS [Title],
        NULL AS [Description],
        NULL AS [ImageUrl],
        N'/chat/' + CAST(um.[ChatId] AS NVARCHAR(36)) AS [LinkUrl],
        0 AS [Type],                    -- NotificationType.ChatMessage = 0
        0 AS [IsRead],                  -- false
        MAX(um.[MessageSentUtc]) AS [CreatedUtc],
        NULL AS [ReadUtc],
        N'Chat' AS [SourceType],
        CAST(um.[ChatId] AS NVARCHAR(36)) AS [SourceId]
    FROM [UnreadMessages] um
    -- Only migrate if no notification already exists for this chat+recipient
    WHERE NOT EXISTS (
        SELECT 1 FROM [Notifications] n
        WHERE n.[RecipientId] = um.[RecipientId]
          AND n.[SourceType] = N'Chat'
          AND n.[SourceId] = CAST(um.[ChatId] AS NVARCHAR(36))
          AND n.[IsRead] = 0
    )
    GROUP BY um.[RecipientId], um.[ChatId];

    -- Report how many notifications were created
    DECLARE @InsertedCount INT = @@ROWCOUNT;
    PRINT 'Migrated ' + CAST(@InsertedCount AS NVARCHAR(10)) + ' unread message groups to notifications.';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorNumber INT = ERROR_NUMBER();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();
    DECLARE @ErrorLine INT = ERROR_LINE();

    PRINT 'Migration failed on line ' + CAST(@ErrorLine AS NVARCHAR(10))
        + ': Error ' + CAST(@ErrorNumber AS NVARCHAR(10))
        + ' - ' + @ErrorMessage;

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
