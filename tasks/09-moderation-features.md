# Moderation Features Implementation

## Overview

Implement a moderation system for the Jordnaer Blazor Server application to block malicious users, remove/block posts and groups, and block IPs.

---

## Phase 1: Database Models & Enums

### 1.1 Create Enums

**File:** `src/shared/Jordnaer.Shared/Database/Enums/ReportReason.cs`

```csharp
namespace Jordnaer.Shared.Database.Enums;

public enum ReportReason
{
    Spam = 0,
    Harassment = 1,
    InappropriateContent = 2,
    Misinformation = 3,
    Scam = 4,
    Other = 99
}
```

**File:** `src/shared/Jordnaer.Shared/Database/Enums/ModerationStatus.cs`

```csharp
namespace Jordnaer.Shared.Database.Enums;

public enum ModerationStatus
{
    Pending = 0,
    UnderReview = 1,
    Resolved = 2,
    Dismissed = 3
}
```

**File:** `src/shared/Jordnaer.Shared/Database/Enums/ModerationAction.cs`

```csharp
namespace Jordnaer.Shared.Database.Enums;

public enum ModerationAction
{
    ContentRemoved = 0,
    UserWarned = 1,
    UserSuspended = 2,
    UserBanned = 3,
    GroupRemoved = 4,
    NoActionNeeded = 99
}
```

### 1.2 Create Entity Models

**File:** `src/shared/Jordnaer.Shared/Database/UserModeration.cs`

```csharp
namespace Jordnaer.Shared.Database;

public class UserModeration
{
    public required string UserProfileId { get; set; }
    public UserProfile? UserProfile { get; set; }

    public bool IsBanned { get; set; }
    public string? BanReason { get; set; }
    public DateTime? BannedAtUtc { get; set; }
    public string? BannedByAdminId { get; set; }

    public bool IsSuspended { get; set; }
    public string? SuspensionReason { get; set; }
    public DateTime? SuspensionEndUtc { get; set; }
    public DateTime? SuspendedAtUtc { get; set; }
    public string? SuspendedByAdminId { get; set; }

    public string? AdminNotes { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
}
```

**File:** `src/shared/Jordnaer.Shared/Database/ContentReport.cs`

```csharp
using Jordnaer.Shared.Database.Enums;

namespace Jordnaer.Shared.Database;

public class ContentReport
{
    public Guid Id { get; set; }

    // What is being reported (only one should be set)
    public Guid? ReportedPostId { get; set; }
    public Post? ReportedPost { get; set; }

    public Guid? ReportedGroupPostId { get; set; }
    public GroupPost? ReportedGroupPost { get; set; }

    public Guid? ReportedGroupId { get; set; }
    public Group? ReportedGroup { get; set; }

    public string? ReportedUserId { get; set; }
    public UserProfile? ReportedUser { get; set; }

    // Who reported it
    public required string ReportedByUserId { get; set; }
    public UserProfile? ReportedByUser { get; set; }

    public ReportReason Reason { get; set; }
    public string? Description { get; set; }

    public ModerationStatus Status { get; set; } = ModerationStatus.Pending;
    public ModerationAction? ActionTaken { get; set; }

    public string? ResolvedByAdminId { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public string? ResolutionNotes { get; set; }

    public DateTime CreatedUtc { get; set; }
}
```

**File:** `src/shared/Jordnaer.Shared/Database/IPBlacklist.cs`

```csharp
namespace Jordnaer.Shared.Database;

public class IPBlacklist
{
    public Guid Id { get; set; }

    public required string IPAddress { get; set; }
    public bool IsBlocked { get; set; } = true;

    public string? BlockReason { get; set; }
    public required string BlockedByAdminId { get; set; }

    public DateTime BlockedAtUtc { get; set; }
    public DateTime? UnblockedAtUtc { get; set; }
}
```

### 1.3 Update DbContext

**File:** `src/web/Jordnaer/Database/JordnaerDbContext.cs`

Add these DbSets:

```csharp
public DbSet<UserModeration> UserModerations => Set<UserModeration>();
public DbSet<ContentReport> ContentReports => Set<ContentReport>();
public DbSet<IPBlacklist> IPBlacklist => Set<IPBlacklist>();
```

Add configuration in `OnModelCreating`:

```csharp
// UserModeration - UserProfileId is PK
modelBuilder.Entity<UserModeration>(entity =>
{
    entity.HasKey(e => e.UserProfileId);
    entity.HasOne(e => e.UserProfile)
          .WithOne()
          .HasForeignKey<UserModeration>(e => e.UserProfileId)
          .OnDelete(DeleteBehavior.Cascade);
});

// ContentReport
modelBuilder.Entity<ContentReport>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => e.Status);
    entity.HasIndex(e => e.ReportedPostId);
    entity.HasIndex(e => e.ReportedGroupPostId);
    entity.HasIndex(e => e.ReportedGroupId);
    entity.HasIndex(e => e.ReportedUserId);
    entity.HasIndex(e => e.ReportedByUserId);

    entity.HasOne(e => e.ReportedPost)
          .WithMany()
          .HasForeignKey(e => e.ReportedPostId)
          .OnDelete(DeleteBehavior.SetNull);

    entity.HasOne(e => e.ReportedGroupPost)
          .WithMany()
          .HasForeignKey(e => e.ReportedGroupPostId)
          .OnDelete(DeleteBehavior.SetNull);

    entity.HasOne(e => e.ReportedGroup)
          .WithMany()
          .HasForeignKey(e => e.ReportedGroupId)
          .OnDelete(DeleteBehavior.SetNull);

    entity.HasOne(e => e.ReportedUser)
          .WithMany()
          .HasForeignKey(e => e.ReportedUserId)
          .OnDelete(DeleteBehavior.SetNull);

    entity.HasOne(e => e.ReportedByUser)
          .WithMany()
          .HasForeignKey(e => e.ReportedByUserId)
          .OnDelete(DeleteBehavior.Cascade);

    entity.Property(e => e.Description).HasMaxLength(2000);
    entity.Property(e => e.ResolutionNotes).HasMaxLength(2000);
});

// IPBlacklist
modelBuilder.Entity<IPBlacklist>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => e.IPAddress);
    entity.HasIndex(e => e.IsBlocked);
    entity.Property(e => e.IPAddress).HasMaxLength(45); // IPv6 max length
    entity.Property(e => e.BlockReason).HasMaxLength(500);
});
```

### 1.4 Create Migration

Run after adding models:

```bash
cd src/web/Jordnaer
dotnet ef migrations add AddModerationFeatures
dotnet ef database update
```

---

## Phase 2: Services

### 2.1 User Moderation Service

**File:** `src/web/Jordnaer/Features/Moderation/UserModerationService.cs`

```csharp
using Jordnaer.Database;
using Jordnaer.Shared.Database;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Jordnaer.Features.Moderation;

public interface IUserModerationService
{
    Task<OneOf<UserModeration, NotFound>> GetModerationStatusAsync(string userId, CancellationToken ct = default);
    Task<OneOf<Success, Error<string>>> BanUserAsync(string userId, string reason, string adminId, CancellationToken ct = default);
    Task<OneOf<Success, Error<string>>> UnbanUserAsync(string userId, CancellationToken ct = default);
    Task<OneOf<Success, Error<string>>> SuspendUserAsync(string userId, string reason, DateTime endDateUtc, string adminId, CancellationToken ct = default);
    Task<OneOf<Success, Error<string>>> UnsuspendUserAsync(string userId, CancellationToken ct = default);
    Task<bool> IsUserBlockedAsync(string userId, CancellationToken ct = default);
    Task<List<UserModeration>> GetAllModerationRecordsAsync(CancellationToken ct = default);
}

public class UserModerationService(IDbContextFactory<JordnaerDbContext> contextFactory, ILogger<UserModerationService> logger) : IUserModerationService
{
    public async Task<OneOf<UserModeration, NotFound>> GetModerationStatusAsync(string userId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        var record = await context.UserModerations
            .AsNoTracking()
            .Include(m => m.UserProfile)
            .FirstOrDefaultAsync(m => m.UserProfileId == userId, ct);

        return record is null ? new NotFound() : record;
    }

    public async Task<OneOf<Success, Error<string>>> BanUserAsync(string userId, string reason, string adminId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var userExists = await context.UserProfiles.AnyAsync(u => u.Id == userId, ct);
        if (!userExists)
            return new Error<string>("User not found");

        var record = await context.UserModerations.FirstOrDefaultAsync(m => m.UserProfileId == userId, ct);
        var now = DateTime.UtcNow;

        if (record is null)
        {
            record = new UserModeration
            {
                UserProfileId = userId,
                IsBanned = true,
                BanReason = reason,
                BannedAtUtc = now,
                BannedByAdminId = adminId,
                CreatedUtc = now,
                LastUpdatedUtc = now
            };
            context.UserModerations.Add(record);
        }
        else
        {
            record.IsBanned = true;
            record.BanReason = reason;
            record.BannedAtUtc = now;
            record.BannedByAdminId = adminId;
            record.LastUpdatedUtc = now;
        }

        await context.SaveChangesAsync(ct);
        logger.LogInformation("User {UserId} banned by admin {AdminId}. Reason: {Reason}", userId, adminId, reason);

        return new Success();
    }

    public async Task<OneOf<Success, Error<string>>> UnbanUserAsync(string userId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var record = await context.UserModerations.FirstOrDefaultAsync(m => m.UserProfileId == userId, ct);
        if (record is null)
            return new Error<string>("No moderation record found");

        record.IsBanned = false;
        record.LastUpdatedUtc = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        logger.LogInformation("User {UserId} unbanned", userId);

        return new Success();
    }

    public async Task<OneOf<Success, Error<string>>> SuspendUserAsync(string userId, string reason, DateTime endDateUtc, string adminId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var userExists = await context.UserProfiles.AnyAsync(u => u.Id == userId, ct);
        if (!userExists)
            return new Error<string>("User not found");

        var record = await context.UserModerations.FirstOrDefaultAsync(m => m.UserProfileId == userId, ct);
        var now = DateTime.UtcNow;

        if (record is null)
        {
            record = new UserModeration
            {
                UserProfileId = userId,
                IsSuspended = true,
                SuspensionReason = reason,
                SuspensionEndUtc = endDateUtc,
                SuspendedAtUtc = now,
                SuspendedByAdminId = adminId,
                CreatedUtc = now,
                LastUpdatedUtc = now
            };
            context.UserModerations.Add(record);
        }
        else
        {
            record.IsSuspended = true;
            record.SuspensionReason = reason;
            record.SuspensionEndUtc = endDateUtc;
            record.SuspendedAtUtc = now;
            record.SuspendedByAdminId = adminId;
            record.LastUpdatedUtc = now;
        }

        await context.SaveChangesAsync(ct);
        logger.LogInformation("User {UserId} suspended by admin {AdminId} until {EndDate}. Reason: {Reason}",
            userId, adminId, endDateUtc, reason);

        return new Success();
    }

    public async Task<OneOf<Success, Error<string>>> UnsuspendUserAsync(string userId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var record = await context.UserModerations.FirstOrDefaultAsync(m => m.UserProfileId == userId, ct);
        if (record is null)
            return new Error<string>("No moderation record found");

        record.IsSuspended = false;
        record.SuspensionEndUtc = null;
        record.LastUpdatedUtc = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        logger.LogInformation("User {UserId} unsuspended", userId);

        return new Success();
    }

    public async Task<bool> IsUserBlockedAsync(string userId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        var record = await context.UserModerations
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserProfileId == userId, ct);

        if (record is null) return false;

        if (record.IsBanned) return true;

        if (record.IsSuspended && record.SuspensionEndUtc > DateTime.UtcNow) return true;

        return false;
    }

    public async Task<List<UserModeration>> GetAllModerationRecordsAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.UserModerations
            .AsNoTracking()
            .Include(m => m.UserProfile)
            .OrderByDescending(m => m.LastUpdatedUtc)
            .ToListAsync(ct);
    }
}
```

### 2.2 Content Report Service

**File:** `src/web/Jordnaer/Features/Moderation/ContentReportService.cs`

```csharp
using Jordnaer.Database;
using Jordnaer.Shared.Database;
using Jordnaer.Shared.Database.Enums;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Jordnaer.Features.Moderation;

public interface IContentReportService
{
    Task<OneOf<Success, Error<string>>> ReportPostAsync(Guid postId, string reportedByUserId, ReportReason reason, string? description, CancellationToken ct = default);
    Task<OneOf<Success, Error<string>>> ReportGroupPostAsync(Guid groupPostId, string reportedByUserId, ReportReason reason, string? description, CancellationToken ct = default);
    Task<OneOf<Success, Error<string>>> ReportGroupAsync(Guid groupId, string reportedByUserId, ReportReason reason, string? description, CancellationToken ct = default);
    Task<OneOf<Success, Error<string>>> ReportUserAsync(string userId, string reportedByUserId, ReportReason reason, string? description, CancellationToken ct = default);
    Task<List<ContentReport>> GetPendingReportsAsync(CancellationToken ct = default);
    Task<List<ContentReport>> GetAllReportsAsync(int skip = 0, int take = 50, CancellationToken ct = default);
    Task<OneOf<Success, Error<string>>> ResolveReportAsync(Guid reportId, string adminId, ModerationAction action, string? notes, CancellationToken ct = default);
    Task<OneOf<Success, Error<string>>> DismissReportAsync(Guid reportId, string adminId, string? notes, CancellationToken ct = default);
}

public class ContentReportService(IDbContextFactory<JordnaerDbContext> contextFactory, ILogger<ContentReportService> logger) : IContentReportService
{
    public async Task<OneOf<Success, Error<string>>> ReportPostAsync(Guid postId, string reportedByUserId, ReportReason reason, string? description, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var postExists = await context.Posts.AnyAsync(p => p.Id == postId, ct);
        if (!postExists)
            return new Error<string>("Post not found");

        var report = new ContentReport
        {
            Id = Guid.NewGuid(),
            ReportedPostId = postId,
            ReportedByUserId = reportedByUserId,
            Reason = reason,
            Description = description,
            Status = ModerationStatus.Pending,
            CreatedUtc = DateTime.UtcNow
        };

        context.ContentReports.Add(report);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Post {PostId} reported by user {UserId}. Reason: {Reason}", postId, reportedByUserId, reason);
        return new Success();
    }

    public async Task<OneOf<Success, Error<string>>> ReportGroupPostAsync(Guid groupPostId, string reportedByUserId, ReportReason reason, string? description, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var postExists = await context.GroupPosts.AnyAsync(p => p.Id == groupPostId, ct);
        if (!postExists)
            return new Error<string>("Group post not found");

        var report = new ContentReport
        {
            Id = Guid.NewGuid(),
            ReportedGroupPostId = groupPostId,
            ReportedByUserId = reportedByUserId,
            Reason = reason,
            Description = description,
            Status = ModerationStatus.Pending,
            CreatedUtc = DateTime.UtcNow
        };

        context.ContentReports.Add(report);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Group post {PostId} reported by user {UserId}. Reason: {Reason}", groupPostId, reportedByUserId, reason);
        return new Success();
    }

    public async Task<OneOf<Success, Error<string>>> ReportGroupAsync(Guid groupId, string reportedByUserId, ReportReason reason, string? description, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var groupExists = await context.Groups.AnyAsync(g => g.Id == groupId, ct);
        if (!groupExists)
            return new Error<string>("Group not found");

        var report = new ContentReport
        {
            Id = Guid.NewGuid(),
            ReportedGroupId = groupId,
            ReportedByUserId = reportedByUserId,
            Reason = reason,
            Description = description,
            Status = ModerationStatus.Pending,
            CreatedUtc = DateTime.UtcNow
        };

        context.ContentReports.Add(report);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Group {GroupId} reported by user {UserId}. Reason: {Reason}", groupId, reportedByUserId, reason);
        return new Success();
    }

    public async Task<OneOf<Success, Error<string>>> ReportUserAsync(string userId, string reportedByUserId, ReportReason reason, string? description, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var userExists = await context.UserProfiles.AnyAsync(u => u.Id == userId, ct);
        if (!userExists)
            return new Error<string>("User not found");

        var report = new ContentReport
        {
            Id = Guid.NewGuid(),
            ReportedUserId = userId,
            ReportedByUserId = reportedByUserId,
            Reason = reason,
            Description = description,
            Status = ModerationStatus.Pending,
            CreatedUtc = DateTime.UtcNow
        };

        context.ContentReports.Add(report);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("User {ReportedUserId} reported by user {UserId}. Reason: {Reason}", userId, reportedByUserId, reason);
        return new Success();
    }

    public async Task<List<ContentReport>> GetPendingReportsAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.ContentReports
            .AsNoTracking()
            .Where(r => r.Status == ModerationStatus.Pending || r.Status == ModerationStatus.UnderReview)
            .Include(r => r.ReportedPost)
            .Include(r => r.ReportedGroupPost)
            .Include(r => r.ReportedGroup)
            .Include(r => r.ReportedUser)
            .Include(r => r.ReportedByUser)
            .OrderBy(r => r.CreatedUtc)
            .ToListAsync(ct);
    }

    public async Task<List<ContentReport>> GetAllReportsAsync(int skip = 0, int take = 50, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.ContentReports
            .AsNoTracking()
            .Include(r => r.ReportedPost)
            .Include(r => r.ReportedGroupPost)
            .Include(r => r.ReportedGroup)
            .Include(r => r.ReportedUser)
            .Include(r => r.ReportedByUser)
            .OrderByDescending(r => r.CreatedUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<OneOf<Success, Error<string>>> ResolveReportAsync(Guid reportId, string adminId, ModerationAction action, string? notes, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var report = await context.ContentReports.FirstOrDefaultAsync(r => r.Id == reportId, ct);
        if (report is null)
            return new Error<string>("Report not found");

        report.Status = ModerationStatus.Resolved;
        report.ActionTaken = action;
        report.ResolvedByAdminId = adminId;
        report.ResolvedAtUtc = DateTime.UtcNow;
        report.ResolutionNotes = notes;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Report {ReportId} resolved by admin {AdminId}. Action: {Action}", reportId, adminId, action);
        return new Success();
    }

    public async Task<OneOf<Success, Error<string>>> DismissReportAsync(Guid reportId, string adminId, string? notes, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var report = await context.ContentReports.FirstOrDefaultAsync(r => r.Id == reportId, ct);
        if (report is null)
            return new Error<string>("Report not found");

        report.Status = ModerationStatus.Dismissed;
        report.ActionTaken = ModerationAction.NoActionNeeded;
        report.ResolvedByAdminId = adminId;
        report.ResolvedAtUtc = DateTime.UtcNow;
        report.ResolutionNotes = notes;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Report {ReportId} dismissed by admin {AdminId}", reportId, adminId);
        return new Success();
    }
}
```

### 2.3 IP Blacklist Service

**File:** `src/web/Jordnaer/Features/Moderation/IPBlacklistService.cs`

```csharp
using Jordnaer.Database;
using Jordnaer.Shared.Database;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Jordnaer.Features.Moderation;

public interface IIPBlacklistService
{
    Task<bool> IsIPBlockedAsync(string ipAddress, CancellationToken ct = default);
    Task<OneOf<Success, Error<string>>> BlockIPAsync(string ipAddress, string reason, string adminId, CancellationToken ct = default);
    Task<OneOf<Success, Error<string>>> UnblockIPAsync(Guid id, CancellationToken ct = default);
    Task<List<IPBlacklist>> GetAllBlockedIPsAsync(CancellationToken ct = default);
}

public class IPBlacklistService(IDbContextFactory<JordnaerDbContext> contextFactory, ILogger<IPBlacklistService> logger) : IIPBlacklistService
{
    public async Task<bool> IsIPBlockedAsync(string ipAddress, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.IPBlacklist
            .AsNoTracking()
            .AnyAsync(ip => ip.IPAddress == ipAddress && ip.IsBlocked, ct);
    }

    public async Task<OneOf<Success, Error<string>>> BlockIPAsync(string ipAddress, string reason, string adminId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var existing = await context.IPBlacklist.FirstOrDefaultAsync(ip => ip.IPAddress == ipAddress, ct);
        if (existing is not null)
        {
            if (existing.IsBlocked)
                return new Error<string>("IP is already blocked");

            existing.IsBlocked = true;
            existing.BlockReason = reason;
            existing.BlockedByAdminId = adminId;
            existing.BlockedAtUtc = DateTime.UtcNow;
            existing.UnblockedAtUtc = null;
        }
        else
        {
            var entry = new IPBlacklist
            {
                Id = Guid.NewGuid(),
                IPAddress = ipAddress,
                IsBlocked = true,
                BlockReason = reason,
                BlockedByAdminId = adminId,
                BlockedAtUtc = DateTime.UtcNow
            };
            context.IPBlacklist.Add(entry);
        }

        await context.SaveChangesAsync(ct);
        logger.LogInformation("IP {IPAddress} blocked by admin {AdminId}. Reason: {Reason}", ipAddress, adminId, reason);

        return new Success();
    }

    public async Task<OneOf<Success, Error<string>>> UnblockIPAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var entry = await context.IPBlacklist.FirstOrDefaultAsync(ip => ip.Id == id, ct);
        if (entry is null)
            return new Error<string>("IP blacklist entry not found");

        entry.IsBlocked = false;
        entry.UnblockedAtUtc = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        logger.LogInformation("IP {IPAddress} unblocked", entry.IPAddress);

        return new Success();
    }

    public async Task<List<IPBlacklist>> GetAllBlockedIPsAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.IPBlacklist
            .AsNoTracking()
            .OrderByDescending(ip => ip.BlockedAtUtc)
            .ToListAsync(ct);
    }
}
```

### 2.4 Content Removal Service

**File:** `src/web/Jordnaer/Features/Moderation/ContentRemovalService.cs`

```csharp
using Jordnaer.Database;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Jordnaer.Features.Moderation;

public interface IContentRemovalService
{
    Task<OneOf<Success, Error<string>>> RemovePostAsync(Guid postId, string adminId, string reason, CancellationToken ct = default);
    Task<OneOf<Success, Error<string>>> RemoveGroupPostAsync(Guid groupPostId, string adminId, string reason, CancellationToken ct = default);
    Task<OneOf<Success, Error<string>>> RemoveGroupAsync(Guid groupId, string adminId, string reason, CancellationToken ct = default);
}

public class ContentRemovalService(IDbContextFactory<JordnaerDbContext> contextFactory, ILogger<ContentRemovalService> logger) : IContentRemovalService
{
    public async Task<OneOf<Success, Error<string>>> RemovePostAsync(Guid postId, string adminId, string reason, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var post = await context.Posts.FirstOrDefaultAsync(p => p.Id == postId, ct);
        if (post is null)
            return new Error<string>("Post not found");

        context.Posts.Remove(post);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Post {PostId} removed by admin {AdminId}. Reason: {Reason}", postId, adminId, reason);
        return new Success();
    }

    public async Task<OneOf<Success, Error<string>>> RemoveGroupPostAsync(Guid groupPostId, string adminId, string reason, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var post = await context.GroupPosts.FirstOrDefaultAsync(p => p.Id == groupPostId, ct);
        if (post is null)
            return new Error<string>("Group post not found");

        context.GroupPosts.Remove(post);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Group post {PostId} removed by admin {AdminId}. Reason: {Reason}", groupPostId, adminId, reason);
        return new Success();
    }

    public async Task<OneOf<Success, Error<string>>> RemoveGroupAsync(Guid groupId, string adminId, string reason, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId, ct);
        if (group is null)
            return new Error<string>("Group not found");

        context.Groups.Remove(group);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Group {GroupId} removed by admin {AdminId}. Reason: {Reason}", groupId, adminId, reason);
        return new Success();
    }
}
```

### 2.5 Register Services

**File:** `src/web/Jordnaer/Program.cs` (add to service registration section)

```csharp
// Moderation services
builder.Services.AddScoped<IUserModerationService, UserModerationService>();
builder.Services.AddScoped<IContentReportService, ContentReportService>();
builder.Services.AddScoped<IIPBlacklistService, IPBlacklistService>();
builder.Services.AddScoped<IContentRemovalService, ContentRemovalService>();
```

---

## Phase 3: Middleware for IP Blocking

**File:** `src/web/Jordnaer/Features/Moderation/IPBlockingMiddleware.cs`

```csharp
using Jordnaer.Features.Moderation;

namespace Jordnaer.Features.Moderation;

public class IPBlockingMiddleware(RequestDelegate next, ILogger<IPBlockingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IIPBlacklistService ipBlacklistService)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();

        if (!string.IsNullOrEmpty(ipAddress))
        {
            var isBlocked = await ipBlacklistService.IsIPBlockedAsync(ipAddress);
            if (isBlocked)
            {
                logger.LogWarning("Blocked request from blacklisted IP: {IPAddress}", ipAddress);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Access denied.");
                return;
            }
        }

        await next(context);
    }
}

public static class IPBlockingMiddlewareExtensions
{
    public static IApplicationBuilder UseIPBlocking(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<IPBlockingMiddleware>();
    }
}
```

**Register in `Program.cs`** (after authentication middleware):

```csharp
app.UseIPBlocking();
```

---

## Phase 4: Admin UI Pages

### 4.1 Content Reports Page

**File:** `src/web/Jordnaer/Pages/Backoffice/ContentReportsPage.razor`

```razor
@page "/backoffice/reports"
@using Jordnaer.Features.Authentication
@using Jordnaer.Features.Moderation
@using Jordnaer.Shared.Database
@using Jordnaer.Shared.Database.Enums
@attribute [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
@inject IContentReportService ReportService
@inject IContentRemovalService RemovalService
@inject IUserModerationService UserModerationService
@inject ISnackbar Snackbar

<PageTitle>Content Reports - Backoffice</PageTitle>

<MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="mt-4">
    <MudText Typo="Typo.h4" Class="mb-4">Content Reports</MudText>

    <MudTabs Elevation="2" Rounded="true" ApplyEffectsToContainer="true" PanelClass="pa-4">
        <MudTabPanel Text="Pending (@_pendingReports.Count)">
            <MudDataGrid Items="_pendingReports" Dense="true" Hover="true">
                <Columns>
                    <PropertyColumn Property="x => x.CreatedUtc" Title="Reported" Format="yyyy-MM-dd HH:mm" />
                    <TemplateColumn Title="Type">
                        <CellTemplate>
                            @GetReportType(context.Item)
                        </CellTemplate>
                    </TemplateColumn>
                    <PropertyColumn Property="x => x.Reason" Title="Reason" />
                    <PropertyColumn Property="x => x.Description" Title="Description" />
                    <TemplateColumn Title="Reported By">
                        <CellTemplate>
                            @(context.Item.ReportedByUser?.UserName ?? "Unknown")
                        </CellTemplate>
                    </TemplateColumn>
                    <TemplateColumn Title="Actions">
                        <CellTemplate>
                            <MudButtonGroup Size="Size.Small">
                                <MudButton Variant="Variant.Outlined" Color="Color.Error" OnClick="@(() => OpenResolveDialog(context.Item))">
                                    Take Action
                                </MudButton>
                                <MudButton Variant="Variant.Outlined" Color="Color.Default" OnClick="@(() => DismissReport(context.Item.Id))">
                                    Dismiss
                                </MudButton>
                            </MudButtonGroup>
                        </CellTemplate>
                    </TemplateColumn>
                </Columns>
            </MudDataGrid>
        </MudTabPanel>

        <MudTabPanel Text="All Reports">
            <MudDataGrid Items="_allReports" Dense="true" Hover="true">
                <Columns>
                    <PropertyColumn Property="x => x.CreatedUtc" Title="Reported" Format="yyyy-MM-dd HH:mm" />
                    <TemplateColumn Title="Type">
                        <CellTemplate>
                            @GetReportType(context.Item)
                        </CellTemplate>
                    </TemplateColumn>
                    <PropertyColumn Property="x => x.Reason" Title="Reason" />
                    <PropertyColumn Property="x => x.Status" Title="Status" />
                    <PropertyColumn Property="x => x.ActionTaken" Title="Action Taken" />
                    <PropertyColumn Property="x => x.ResolvedAtUtc" Title="Resolved" Format="yyyy-MM-dd HH:mm" />
                </Columns>
            </MudDataGrid>
        </MudTabPanel>
    </MudTabs>
</MudContainer>

@code {
    private List<ContentReport> _pendingReports = new();
    private List<ContentReport> _allReports = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadReports();
    }

    private async Task LoadReports()
    {
        _pendingReports = await ReportService.GetPendingReportsAsync();
        _allReports = await ReportService.GetAllReportsAsync();
    }

    private string GetReportType(ContentReport report)
    {
        if (report.ReportedPostId.HasValue) return "Post";
        if (report.ReportedGroupPostId.HasValue) return "Group Post";
        if (report.ReportedGroupId.HasValue) return "Group";
        if (!string.IsNullOrEmpty(report.ReportedUserId)) return "User";
        return "Unknown";
    }

    private async Task DismissReport(Guid reportId)
    {
        // TODO: Get current admin ID from CurrentUser
        var result = await ReportService.DismissReportAsync(reportId, "admin", null);
        result.Switch(
            success => { Snackbar.Add("Report dismissed", Severity.Success); },
            error => { Snackbar.Add(error.Value, Severity.Error); }
        );
        await LoadReports();
    }

    private void OpenResolveDialog(ContentReport report)
    {
        // TODO: Implement dialog for taking moderation action
    }
}
```

### 4.2 User Moderation Page

**File:** `src/web/Jordnaer/Pages/Backoffice/UserModerationPage.razor`

```razor
@page "/backoffice/user-moderation"
@using Jordnaer.Features.Authentication
@using Jordnaer.Features.Moderation
@using Jordnaer.Shared.Database
@attribute [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
@inject IUserModerationService ModerationService
@inject IDialogService DialogService
@inject ISnackbar Snackbar

<PageTitle>User Moderation - Backoffice</PageTitle>

<MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="mt-4">
    <MudText Typo="Typo.h4" Class="mb-4">User Moderation</MudText>

    <MudDataGrid Items="_moderationRecords" Dense="true" Hover="true">
        <Columns>
            <TemplateColumn Title="User">
                <CellTemplate>
                    @(context.Item.UserProfile?.UserName ?? context.Item.UserProfileId)
                </CellTemplate>
            </TemplateColumn>
            <TemplateColumn Title="Status">
                <CellTemplate>
                    @if (context.Item.IsBanned)
                    {
                        <MudChip T="string" Color="Color.Error" Size="Size.Small">Banned</MudChip>
                    }
                    else if (context.Item.IsSuspended && context.Item.SuspensionEndUtc > DateTime.UtcNow)
                    {
                        <MudChip T="string" Color="Color.Warning" Size="Size.Small">Suspended until @context.Item.SuspensionEndUtc?.ToString("yyyy-MM-dd")</MudChip>
                    }
                    else
                    {
                        <MudChip T="string" Color="Color.Success" Size="Size.Small">Active</MudChip>
                    }
                </CellTemplate>
            </TemplateColumn>
            <PropertyColumn Property="x => x.BanReason ?? x.SuspensionReason" Title="Reason" />
            <PropertyColumn Property="x => x.LastUpdatedUtc" Title="Last Updated" Format="yyyy-MM-dd HH:mm" />
            <TemplateColumn Title="Actions">
                <CellTemplate>
                    @if (context.Item.IsBanned)
                    {
                        <MudButton Size="Size.Small" Variant="Variant.Outlined" Color="Color.Success" OnClick="@(() => UnbanUser(context.Item.UserProfileId))">
                            Unban
                        </MudButton>
                    }
                    else if (context.Item.IsSuspended)
                    {
                        <MudButton Size="Size.Small" Variant="Variant.Outlined" Color="Color.Success" OnClick="@(() => UnsuspendUser(context.Item.UserProfileId))">
                            Unsuspend
                        </MudButton>
                    }
                </CellTemplate>
            </TemplateColumn>
        </Columns>
    </MudDataGrid>
</MudContainer>

@code {
    private List<UserModeration> _moderationRecords = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadRecords();
    }

    private async Task LoadRecords()
    {
        _moderationRecords = await ModerationService.GetAllModerationRecordsAsync();
    }

    private async Task UnbanUser(string userId)
    {
        var result = await ModerationService.UnbanUserAsync(userId);
        result.Switch(
            success => { Snackbar.Add("User unbanned", Severity.Success); },
            error => { Snackbar.Add(error.Value, Severity.Error); }
        );
        await LoadRecords();
    }

    private async Task UnsuspendUser(string userId)
    {
        var result = await ModerationService.UnsuspendUserAsync(userId);
        result.Switch(
            success => { Snackbar.Add("User unsuspended", Severity.Success); },
            error => { Snackbar.Add(error.Value, Severity.Error); }
        );
        await LoadRecords();
    }
}
```

### 4.3 IP Blacklist Page

**File:** `src/web/Jordnaer/Pages/Backoffice/IPBlacklistPage.razor`

```razor
@page "/backoffice/ip-blacklist"
@using Jordnaer.Features.Authentication
@using Jordnaer.Features.Moderation
@using Jordnaer.Shared.Database
@attribute [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
@inject IIPBlacklistService BlacklistService
@inject ISnackbar Snackbar

<PageTitle>IP Blacklist - Backoffice</PageTitle>

<MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="mt-4">
    <MudStack Row="true" Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center" Class="mb-4">
        <MudText Typo="Typo.h4">IP Blacklist</MudText>
        <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="OpenAddDialog">
            Block IP
        </MudButton>
    </MudStack>

    <MudPaper Class="pa-4 mb-4">
        <MudStack Row="true" Spacing="2">
            <MudTextField @bind-Value="_newIP" Label="IP Address" Variant="Variant.Outlined" />
            <MudTextField @bind-Value="_newReason" Label="Reason" Variant="Variant.Outlined" />
            <MudButton Variant="Variant.Filled" Color="Color.Error" OnClick="BlockIP">Block</MudButton>
        </MudStack>
    </MudPaper>

    <MudDataGrid Items="_blacklist" Dense="true" Hover="true">
        <Columns>
            <PropertyColumn Property="x => x.IPAddress" Title="IP Address" />
            <TemplateColumn Title="Status">
                <CellTemplate>
                    @if (context.Item.IsBlocked)
                    {
                        <MudChip T="string" Color="Color.Error" Size="Size.Small">Blocked</MudChip>
                    }
                    else
                    {
                        <MudChip T="string" Color="Color.Default" Size="Size.Small">Unblocked</MudChip>
                    }
                </CellTemplate>
            </TemplateColumn>
            <PropertyColumn Property="x => x.BlockReason" Title="Reason" />
            <PropertyColumn Property="x => x.BlockedAtUtc" Title="Blocked At" Format="yyyy-MM-dd HH:mm" />
            <TemplateColumn Title="Actions">
                <CellTemplate>
                    @if (context.Item.IsBlocked)
                    {
                        <MudButton Size="Size.Small" Variant="Variant.Outlined" Color="Color.Success" OnClick="@(() => UnblockIP(context.Item.Id))">
                            Unblock
                        </MudButton>
                    }
                </CellTemplate>
            </TemplateColumn>
        </Columns>
    </MudDataGrid>
</MudContainer>

@code {
    private List<IPBlacklist> _blacklist = new();
    private string _newIP = string.Empty;
    private string _newReason = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadBlacklist();
    }

    private async Task LoadBlacklist()
    {
        _blacklist = await BlacklistService.GetAllBlockedIPsAsync();
    }

    private async Task BlockIP()
    {
        if (string.IsNullOrWhiteSpace(_newIP))
        {
            Snackbar.Add("Please enter an IP address", Severity.Warning);
            return;
        }

        // TODO: Get current admin ID from CurrentUser
        var result = await BlacklistService.BlockIPAsync(_newIP, _newReason, "admin");
        result.Switch(
            success => {
                Snackbar.Add("IP blocked", Severity.Success);
                _newIP = string.Empty;
                _newReason = string.Empty;
            },
            error => { Snackbar.Add(error.Value, Severity.Error); }
        );
        await LoadBlacklist();
    }

    private async Task UnblockIP(Guid id)
    {
        var result = await BlacklistService.UnblockIPAsync(id);
        result.Switch(
            success => { Snackbar.Add("IP unblocked", Severity.Success); },
            error => { Snackbar.Add(error.Value, Severity.Error); }
        );
        await LoadBlacklist();
    }

    private void OpenAddDialog()
    {
        // Focus on the IP input field
    }
}
```

---

## Phase 5: User-Facing Report Button

### 5.1 Report Content Dialog

**File:** `src/web/Jordnaer/Features/Moderation/ReportContentDialog.razor`

```razor
@using Jordnaer.Features.Moderation
@using Jordnaer.Shared.Database.Enums

<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">Report Content</MudText>
    </TitleContent>
    <DialogContent>
        <MudStack Spacing="3">
            <MudText>Why are you reporting this content?</MudText>
            <MudRadioGroup @bind-Value="_selectedReason">
                <MudRadio Value="ReportReason.Spam">Spam</MudRadio>
                <MudRadio Value="ReportReason.Harassment">Harassment</MudRadio>
                <MudRadio Value="ReportReason.InappropriateContent">Inappropriate Content</MudRadio>
                <MudRadio Value="ReportReason.Misinformation">Misinformation</MudRadio>
                <MudRadio Value="ReportReason.Scam">Scam</MudRadio>
                <MudRadio Value="ReportReason.Other">Other</MudRadio>
            </MudRadioGroup>
            <MudTextField @bind-Value="_description" Label="Additional details (optional)" Lines="3" Variant="Variant.Outlined" />
        </MudStack>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Error" Variant="Variant.Filled" OnClick="Submit">Report</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public Guid? PostId { get; set; }

    [Parameter]
    public Guid? GroupPostId { get; set; }

    [Parameter]
    public Guid? GroupId { get; set; }

    [Parameter]
    public string? UserId { get; set; }

    private ReportReason _selectedReason = ReportReason.Spam;
    private string _description = string.Empty;

    private void Cancel() => MudDialog.Cancel();

    private void Submit()
    {
        MudDialog.Close(DialogResult.Ok(new ReportDialogResult(_selectedReason, _description)));
    }

    public record ReportDialogResult(ReportReason Reason, string Description);
}
```

### 5.2 Add Report Button to Posts

In existing post components (e.g., `PostCard.razor`), add a report button:

```razor
<MudIconButton Icon="@Icons.Material.Filled.Flag" Size="Size.Small" OnClick="@(() => OpenReportDialog())" Title="Report" />
```

And the handler:

```csharp
@inject IDialogService DialogService
@inject IContentReportService ReportService
@inject CurrentUser CurrentUser

private async Task OpenReportDialog()
{
    var parameters = new DialogParameters<ReportContentDialog>
    {
        { x => x.PostId, Post.Id }
    };

    var dialog = await DialogService.ShowAsync<ReportContentDialog>("Report Content", parameters);
    var result = await dialog.Result;

    if (!result.Canceled && result.Data is ReportContentDialog.ReportDialogResult data)
    {
        await ReportService.ReportPostAsync(Post.Id, CurrentUser.Id, data.Reason, data.Description);
        Snackbar.Add("Thank you for reporting. We'll review this content.", Severity.Info);
    }
}
```

---

## Phase 6: Middleware for Banned User Check

**File:** `src/web/Jordnaer/Features/Moderation/UserBlockingMiddleware.cs`

```csharp
using Jordnaer.Features.Moderation;
using System.Security.Claims;

namespace Jordnaer.Features.Moderation;

public class UserBlockingMiddleware(RequestDelegate next, ILogger<UserBlockingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IUserModerationService moderationService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var isBlocked = await moderationService.IsUserBlockedAsync(userId);
                if (isBlocked)
                {
                    logger.LogWarning("Blocked request from banned/suspended user: {UserId}", userId);

                    // Redirect to a "banned" page or sign out
                    context.Response.Redirect("/account/banned");
                    return;
                }
            }
        }

        await next(context);
    }
}

public static class UserBlockingMiddlewareExtensions
{
    public static IApplicationBuilder UseUserBlocking(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserBlockingMiddleware>();
    }
}
```

---

## Summary Checklist

- [ ] **Phase 1:** Create enums (ReportReason, ModerationStatus, ModerationAction)
- [ ] **Phase 1:** Create entities (UserModeration, ContentReport, IPBlacklist)
- [ ] **Phase 1:** Update JordnaerDbContext with DbSets and configurations
- [ ] **Phase 1:** Run `dotnet ef migrations add AddModerationFeatures`
- [ ] **Phase 2:** Create UserModerationService
- [ ] **Phase 2:** Create ContentReportService
- [ ] **Phase 2:** Create IPBlacklistService
- [ ] **Phase 2:** Create ContentRemovalService
- [ ] **Phase 2:** Register services in Program.cs
- [ ] **Phase 3:** Create IPBlockingMiddleware
- [ ] **Phase 3:** Register middleware in Program.cs
- [ ] **Phase 4:** Create ContentReportsPage.razor
- [ ] **Phase 4:** Create UserModerationPage.razor
- [ ] **Phase 4:** Create IPBlacklistPage.razor
- [ ] **Phase 5:** Create ReportContentDialog.razor
- [ ] **Phase 5:** Add report buttons to post/group/user components
- [ ] **Phase 6:** Create UserBlockingMiddleware
- [ ] **Phase 6:** Create banned user page

---

## Key Patterns Used

| Pattern | Example File |
|---------|--------------|
| Service with IDbContextFactory | `PostService.cs` |
| OneOf return types | `GroupService.cs` |
| Authorization policies | `AuthorizationPolicies.cs` |
| Admin pages | `UserClaimManagementPage.razor` |
| MudBlazor dialogs | Existing dialog components |
| Middleware | `RateLimitExtensions.cs` |
